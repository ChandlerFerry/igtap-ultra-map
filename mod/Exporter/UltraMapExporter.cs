using System;
using System.IO;
using Recharge.ModApi;
using UnityEngine;

public class UltraMapExporter : IRechargeMod
{
    const string Row = "UltraMapExportRow";

    public string Id => "chandlerferry.ultramap";
    public string DisplayName => "Ultra Map Exporter";
    public Version Version => new Version(1, 0, 0);

    readonly MapBuilder _builder = new MapBuilder();

    public void OnLoad(IRechargeHost host)
    {
        string shown = "";
        void SetLabel(string text) => PauseMenuHelper.AddRow(host.PauseMenu, Row, text, Build);
        void Build()
        {
            if (_builder.Running) return;
            try
            {
                string data = host.ModDataDir(Id);
                var config = host.LoadConfig<MapBuilder.Config>(Id);
                host.SaveConfig(Id, config);
                string trace = WorldExport.Write(data);
                host.Log("Exported " + trace);
                string game = Path.GetDirectoryName(Application.dataPath);
                _builder.Start(config, game, trace, Path.Combine(data, "ultras.json"), Path.Combine(data, "build.log"));
            }
            catch (Exception ex)
            {
                host.LogError(ex.Message);
                SetLabel("Ultra map: " + ex.Message);
            }
        }
        host.OnUpdate += () =>
        {
            string status = _builder.Status;
            if (status == shown) return;
            shown = status;
            if (!_builder.Running) host.Log(status);
            SetLabel("Ultra map: " + status);
        };
        SetLabel("Build ultra map");
    }

    public void OnUnload() { }
}
