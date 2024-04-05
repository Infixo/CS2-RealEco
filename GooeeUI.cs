using Gooee.Plugins.Attributes;
using Gooee.Plugins;

namespace RealEco.Gooee;

public class RealEcoModel : Model
{
    // simple data to control the window visibility
    public bool IsVisibleCommercial { get; set; }
}


public partial class RealEcoController : Controller<RealEcoModel>
{
    public override RealEcoModel Configure()
    {
        return new RealEcoModel();
    }

    [OnTrigger]
    private void OnToggleVisible(string key)
    {
        // stub method to avoid errors
    }

    [OnTrigger]
    private void OnToggleVisibleCommercial(string key)
    {
        base.Model.IsVisibleCommercial = !Model.IsVisibleCommercial;
        base.TriggerUpdate();
    }
}

[ControllerTypes(typeof(RealEcoController))]
[PluginToolbar(typeof(RealEcoController), "RealEco.ui_src.gooee-menu.json")]
public class RealEcoGooeePluginWithControllers : IGooeePluginWithControllers, IGooeeStyleSheet
{
    public string Name => "RealEco";
    public string Version => "0.8.0";
    public string ScriptResource => "RealEco.dist.ui.js";
    public string StyleResource => "RealEco.dist.ui.css";

    public IController[] Controllers { get; set; }

}
