using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace RealEco
{

    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(RealEco)}").SetShowsErrorsInUI(false);

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN());
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
        }
    }
}
