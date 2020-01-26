using Newtonsoft.Json.Linq;
using PX.Data;
using PX.SM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Xml;
using System.Xml.Linq;
using VX.EditorServices.OmniSharp;

namespace VX.EditorServices
{
    public static class CustomizationProjectUtils
    {
        private const string DateTimeSerializationFormat = "MM/dd/yyyy hh:mm:ss.fff tt";

        public static string GetOmniSharpFilePath(string projectName)
        {
            return HostingEnvironment.MapPath($"~/CstDesigner/{projectName.Trim()}_OmniSharp");
        }

        internal static void SaveCustomizationFilesToFolder(Guid customizationProjectId, IFileSystemNotifier fileSystemNotifier)
        {
            if (customizationProjectId != Guid.Empty)
            {
                string customizationFolder = String.Empty;
                bool updateProject = false;

                var custProject = (CustProject)PXSelect<CustProject, Where<CustProject.projID, Equal<Required<CustProject.projID>>>>.Select(new PXGraph(), customizationProjectId);
                if (custProject == null) throw new Exception($"Customization project {customizationProjectId} not found.");

                customizationFolder = CustomizationProjectUtils.GetOmniSharpFilePath(custProject.Name);

                if (!Directory.Exists(customizationFolder))
                {
                    Directory.CreateDirectory(customizationFolder);
                }
                Dictionary<Guid, CustObjectTimestamp> timestamps;
                var processedCustObjects = new HashSet<Guid>();

                var timestampsFilePath = Path.Combine(customizationFolder, "Timestamp.txt");
                if (File.Exists(timestampsFilePath))
                {
                    timestamps = LoadObjectTimestamps(timestampsFilePath);
                }
                else
                {
                    timestamps = new Dictionary<Guid, CustObjectTimestamp>();
                }

                var persistObjectFromXmlMethod = Type.GetType("Customization.CstDocument, PX.Web.Customization").GetMethod("PersistObjectFromXml");
                var saveFilesMethod = Type.GetType("Customization.IPersistObject, PX.Web.Customization").GetMethod("SaveFiles");
                var filesCollectionType = Type.GetType("Customization.FilesCollection, PX.Web.Customization");

                var xmlContents = new XmlDocument();
                foreach (CustObject custObject in PXSelect<CustObject, Where<CustObject.projectID, Equal<Required<CustObject.projectID>>, And<CustObject.isDisabled, Equal<False>>>>.Select(new PXGraph(), customizationProjectId))
                {
                    processedCustObjects.Add(custObject.ObjectID.Value);

                    CustObjectTimestamp timestamp = null;
                    if (timestamps.TryGetValue(custObject.ObjectID.Value, out timestamp))
                    {
                        //Existing file - verify if customization object has been updated before we continue
                        if (timestamp.LastModifiedDateTime == custObject.LastModifiedDateTime.Value)
                            continue;
                    }
                    else
                    {
                        //New file
                        timestamp = new CustObjectTimestamp();
                        timestamps[custObject.ObjectID.Value] = timestamp;
                    }

                    timestamp.LastModifiedDateTime = custObject.LastModifiedDateTime;

                    //Deserialize XML to CustObject and generate customization files
                    xmlContents.LoadXml(custObject.Content);
                    var deserializedCustObject = persistObjectFromXmlMethod.Invoke(null, new object[] { (XmlElement)xmlContents.FirstChild });
                    var filesCollection = Activator.CreateInstance(filesCollectionType);
                    saveFilesMethod.Invoke(deserializedCustObject, new object[] { filesCollection });

                    //Save customization files to disk
                    foreach (/*CustomizedFile*/ object file in (IEnumerable<object>)filesCollection.GetType().GetProperty("Files").GetValue(filesCollection))
                    {
                        updateProject = true;
                        var targetPath = SaveCustomizationFile(file, customizationFolder, fileSystemNotifier);
                        timestamp.Files[targetPath] = true;
                    }
                }

                if (CleanupDeletedCustObjects(timestamps, processedCustObjects, fileSystemNotifier))
                {
                    updateProject = true;
                }

                SaveObjectTimestamps(timestamps, timestampsFilePath);

                if (updateProject)
                {
                    GenerateProjectFile(customizationFolder, fileSystemNotifier);
                }
            }
            else
            {
                //Hackathon: generate dummy Console.cs file
                //var customizationFolder = CustomizationProjectUtils.GetOmniSharpFilePath("Console");

                //if (!Directory.Exists(customizationFolder))
                //{
                //    Directory.CreateDirectory(customizationFolder);
                //}

                //using (File.Create(Path.Combine(customizationFolder, "Console.cs"))) { }
                
                ////Do we need to update it all the time?
                //GenerateProjectFile(customizationFolder, fileSystemNotifier);
        }
    }

        private static bool CleanupDeletedCustObjects(Dictionary<Guid, CustObjectTimestamp> timestamps, HashSet<Guid> processedCustObjects, IFileSystemNotifier fileSystemNotifier)
        {
            List<Guid> removals = new List<Guid>();
            foreach (var kv in timestamps)
            {
                if (!processedCustObjects.Contains(kv.Key))
                {
                    //This customization object no longer exists in the database, delete every file on disk
                    removals.Add(kv.Key);
                    foreach (var path in kv.Value.Files.Keys)
                    {
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                            fileSystemNotifier.Notify(path, FileChangeType.Delete);
                        }
                    }
                }
            }

            if (removals.Count > 0)
            {
                foreach (var key in removals)
                {
                    timestamps.Remove(key);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private static string SaveCustomizationFile(object file, string customizationFolder, IFileSystemNotifier fileSystemNotifier)
        {
            string targetPath = Path.Combine(customizationFolder, (string)file.GetType().GetField("TargetRelativePath").GetValue(file));
            string directoryName = Path.GetDirectoryName(targetPath);

            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);

            FileChangeType changeType = File.Exists(targetPath) ? FileChangeType.Change : FileChangeType.Create;

            if ((bool)file.GetType().GetProperty("IsBinary").GetValue(file))
            {
                using (var fs = File.OpenWrite(targetPath))
                using (var writer = new BinaryWriter(fs))
                {
                    writer.Write((byte[])file.GetType().GetField("ContentBin").GetValue(file));
                }
            }
            else
            {
                using (var writer = File.CreateText(targetPath))
                {
                    writer.Write((string)file.GetType().GetProperty("Content").GetValue(file));
                }
            }

            fileSystemNotifier.Notify(targetPath, changeType);

            return targetPath;
        }

        private static void GenerateProjectFile(string path, IFileSystemNotifier fileSystemNotifier)
        {
            var references = Directory.GetFiles(HostingEnvironment.MapPath($"~/Bin"), "*.dll", SearchOption.TopDirectoryOnly).ToDictionary(p => Path.GetFileNameWithoutExtension(p));
            var projectBinPath = Path.Combine(path, "bin");
            if (Directory.Exists(projectBinPath))
            {
                foreach (var projectDllFile in Directory.GetFiles(projectBinPath, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    //DLL files included in this customization project take precedence over already published references; dictionary value will be overwritten if it exists already
                    references[Path.GetFileNameWithoutExtension(projectDllFile)] = projectDllFile;
                }
            }

            //TODO: Consider including App_RuntimeCode of other *published* projects
            var xml = new XElement("Project",
                new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                new XElement("PropertyGroup",
                    new XElement("OutputType", "Library"),
                    new XElement("TargetFramework", "net471")
                ),
                new XElement("ItemGroup", references.Select(kv =>
                    new XElement("Reference",
                        new XAttribute("Include", kv.Key),
                        new XElement("HintPath", kv.Value)
                    )
                )
            ));

            var projectFilePath = Path.Combine(path, Path.GetFileName(path) + ".csproj");
            xml.Save(projectFilePath);
            fileSystemNotifier.Notify(projectFilePath, FileChangeType.Change);
        }


        private static Dictionary<Guid, CustObjectTimestamp> LoadObjectTimestamps(string path)
        {
            JObject json = JObject.Parse(File.ReadAllText(path));
            var timestamps = new Dictionary<Guid, CustObjectTimestamp>();

            foreach (var custObject in json["objects"])
            {
                var ts = new CustObjectTimestamp();
                ts.LastModifiedDateTime = DateTime.ParseExact(custObject["lastModifiedDateTime"].ToString(), DateTimeSerializationFormat, System.Globalization.CultureInfo.InvariantCulture);
                foreach (var file in custObject["files"])
                {
                    ts.Files.Add(file["path"].ToString(), false);
                }

                timestamps.Add(Guid.Parse(custObject["id"].ToString()), ts);
            }

            return timestamps;
        }

        private static void SaveObjectTimestamps(Dictionary<Guid, CustObjectTimestamp> timestamps, string path)
        {
            JObject json =
                new JObject(
                    new JProperty("objects",
                        new JArray(
                            from t in timestamps
                            select new JObject(
                                new JProperty("id", t.Key),
                                new JProperty("lastModifiedDateTime", t.Value.LastModifiedDateTime.Value.ToString(DateTimeSerializationFormat, System.Globalization.CultureInfo.InvariantCulture)),
                                new JProperty("files",
                                    new JArray(
                                        from f in t.Value.Files
                                        select new JObject(
                                            new JProperty("path", f.Key))
                                        )
                                    )
                                )
                            )
                        )
                    );

            File.WriteAllText(path, json.ToString());
        }
    }
}
