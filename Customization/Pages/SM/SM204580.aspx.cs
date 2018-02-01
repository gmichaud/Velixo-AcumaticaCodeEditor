using System;
using System.Web;
using PX.Common;
using PX.Data;
using PX.SM;
using PX.Web.Customization;
using PX.Web.UI;

[Customization.CstDesignMode(Disabled = true)]
public partial class Pages_VX_VX204580 : PXPage
{
	protected override void OnInit(EventArgs e)
	{

		this.Master.FindControl("usrCaption").Visible = false;
		ProjectBrowserMaint.InitSessionFromQueryString(HttpContext.Current);


		((IPXMasterPage)this.Master).CustomizationAvailable = false;
		base.OnInit(e);
	}

	protected void Page_Load(object sender, EventArgs e)
	{
		
	}

	/// <summary>
	/// The page PreRenderComplete event handler.
	/// </summary>
	protected override void OnPreRenderComplete(EventArgs e)
	{
		string query = ProjectBrowserMaint.ContextCodeFile;
		if (!string.IsNullOrEmpty(query))
		{
			this.ClientScript.RegisterStartupScript(this.GetType(), "query", 
				string.Format("\nvar __queryString = '{0}={1}'; ", "CodeFile", query.Replace('#', '*')), true);
		}
		base.OnPreRenderComplete(e);
	}

	public string GetScriptName(string rname)
	{
		string resource = "PX.Web.Customization.Controls.cseditor." + rname;
		string url = ClientScript.GetWebResourceUrl(typeof(Customization.WebsiteEntryPoints), resource);
		url = url.Replace(".axd?", ".axd?file=" + rname + "&");
		return HttpUtility.HtmlAttributeEncode(url);
	}
    
    public string FileName
    {
        get
        {
            var graph = (GraphCodeFiles) ds.DataGraph;
            if (graph == null) return null;

            var custProject = (CustProject)PXSelect<CustProject, Where<CustProject.projID, Equal<Current<FilterCodeFile.projectID>>>>.Select(graph);
            if (custProject == null) return null;

            //ProjectBrowserMaint.ContextCodeFile is set from CustObject.Name, which looks like Code#POOrderEntry for a file to be edited
            string fileName = ProjectBrowserMaint.ContextCodeFile.Split('#')[1] + ".cs";

            //Similar logic for file name in CstCodeFile.FilePath; replicated here since there's no clean way to invoke it directly
            if (WebConfig.UseRuntimeCompilation)
            {
                return VX.EditorServices.CustomizationProjectUtils.GetOmniSharpFilePath(custProject.Name) + @"\App_RuntimeCode\" + fileName;
            }
            else
            {
                return VX.EditorServices.CustomizationProjectUtils.GetOmniSharpFilePath(custProject.Name) + @"\App_Code\Caches\" + fileName;
            }
        }
    }

    public Guid? ProjectID
    {
        get
        {
            var graph = (GraphCodeFiles) ds.DataGraph;
            if (graph == null) return null;
            return graph.Filter.Current.ProjectID;
        }
    }
}
