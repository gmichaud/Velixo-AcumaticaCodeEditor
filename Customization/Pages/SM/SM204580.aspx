<%@ Page Title="Code Editor" Language="C#" MasterPageFile="~/MasterPages/FormDetail.master"
	AutoEventWireup="true" CodeFile="SM204580.aspx.cs" Inherits="Pages_VX_VX204580"
	EnableViewStateMac="False" EnableViewState="False" ValidateRequest="False" %>

<asp:Content ID="Content1" ContentPlaceHolderID="phDS" runat="Server"> 
	<script type="text/javascript" language="javascript">
        var fileName = "<%=HttpUtility.JavaScriptStringEncode(FileName)%>";
        var projectID = "<%=ProjectID%>";
	</script>

    <script src="../../Scripts/Monaco/acumatica.js" type="text/javascript"></script>

	<px:PXFormView runat="server" SkinID="transparent" ID="formTitle" 
		DataSourceID="ds" DataMember="ViewPageTitle" Width="100%">
		<Template>
			<px:PXTextEdit runat="server" ID="PageTitle" DataField="PageTitle" SelectOnFocus="False"
				SkinID="Label" SuppressLabel="true"
				Width="90%"
				style="padding: 10px">
				<font size="14pt" names="Arial,sans-serif;"/>
			</px:PXTextEdit>
		</Template>
	</px:PXFormView>
	
	<px:PXDataSource ID="ds" runat="server" Visible="true" TypeName="PX.SM.GraphCodeFiles"
		PrimaryView="Filter" PageLoadBehavior="PopulateSavedValues">
		<CallbackCommands>
		</CallbackCommands>
		<ClientEvents Initialize="OnDsInit" />
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="FormFilter" runat="server" DataMember="Filter" DataSourceID="ds"
		Width="100%"  Caption="Project" Style="display: none;visibility: hidden;" Height="0px">
		<Template>
			<px:PXLayoutRule runat="server" ControlSize="L" LabelsWidth="SM" StartColumn="True"/>
		</Template>
	</px:PXFormView>
	<px:PXFormView ID="FormEditContent" runat="server" DataMember="Files" DataSourceID="ds"
		Style="position: absolute; left: 0px; top: 0px; width: 200px; display: none;
		visibility: hidden">
		<Template>
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM" />
			<px:PXTextEdit SuppressLabel="True" Height="20px" ID="EventEditBox" runat="server"
				DataField="FileContent" TextMode="MultiLine" Font-Names="Courier New" Font-Size="10pt"
				Wrap="False" SelectOnFocus="False">
				<ClientEvents Initialize="ActivateMonacoEditor" />
			</px:PXTextEdit></Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXSmartPanel ID="PanelSource" runat="server" Style="width: 100%; height: 300px;"
		RenderVisible="True" Position="Original" AllowMove="False" AllowResize="False"
		AutoSize-Enabled="True" AutoSize-Container="Window" Overflow="Hidden"
		SkinID="Transparent">

		<div id="SourcePlaceholder" style="width: 100%; height: 100%;">
		</div>
	</px:PXSmartPanel>
</asp:Content>

<asp:Content ID="Dialogs" ContentPlaceHolderID="phDialogs" runat="Server">
	<px:PXSmartPanel runat="server" ID="ViewBaseMethod" Width="600px" Height="400px" CaptionVisible="True"
		Caption="Select Methods to Override" ShowMaximizeButton="True" Overflow="Hidden" Key="ViewBaseMethod" AutoRepaint="True">

		<px:PXGrid runat="server"
			ID="gridAddFile" DataSourceID="ds"
			Width="100%" Height="200px" BatchUpdate="True"
			AutoAdjustColumns="True"
			SkinID="Details">
			<Levels>
				<px:PXGridLevel DataMember="ViewBaseMethod">
					<Columns>
						<px:PXGridColumn DataField="Selected" Type="CheckBox" Width="50px" />
						<px:PXGridColumn DataField="DeclaringType" Width="100px" />
						<px:PXGridColumn DataField="Name" Width="300px" />

					</Columns>
				</px:PXGridLevel>
			</Levels>
			<AutoSize Enabled="True" />
			<ActionBar Position="Top" ActionsVisible="False">
				<Actions>
					<AddNew MenuVisible="False" ToolBarVisible="False" />
					<Delete MenuVisible="False" ToolBarVisible="False" />
					<AdjustColumns ToolBarVisible="False" />
					<ExportExcel ToolBarVisible="False" />
				</Actions>

			</ActionBar>
		</px:PXGrid>

		<px:PXPanel ID="PXPanel1" runat="server" SkinID="Buttons">
			<px:PXButton ID="PXButton1" runat="server" DialogResult="OK" Text="Save" />
			<px:PXButton ID="PXButton2" runat="server" DialogResult="No" Text="Cancel" CausesValidation="False" />
		</px:PXPanel>
	</px:PXSmartPanel>
    	<px:PXSmartPanel ID="DlgActionWizard" runat="server"
		CaptionVisible="True"
		Caption="Create Action"
		AutoRepaint="True"
		Key="ViewActionWizard">
		<px:PXFormView ID="FormActionWizard" runat="server"
			SkinID="Transparent" DataMember="ViewActionWizard">
			<Template>
				<px:PXLayoutRule ID="PXLayoutRule11" runat="server" StartColumn="True" />

				<px:PXTextEdit runat="server" ID="ActionName" DataField="ActionName" />
				<px:PXTextEdit runat="server" ID="DisplayName" DataField="DisplayName"/>
				

			</Template>
		</px:PXFormView>

		<px:PXLayoutRule ID="PXLayoutRule8" runat="server" StartRow="True" />
		<px:PXPanel ID="PXPanel7" runat="server" SkinID="Buttons">
			<px:PXButton ID="PXButton13" runat="server" DialogResult="OK" Text="OK">
			</px:PXButton>

			<px:PXButton ID="PXButton14" runat="server" DialogResult="Cancel" Text="Cancel" CausesValidation="False">
			</px:PXButton>
		</px:PXPanel>
	</px:PXSmartPanel>
</asp:Content>
