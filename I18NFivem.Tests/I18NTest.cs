namespace I18NFivem.Tests
{
    using System.Threading.Tasks;
    using CitizenFX.Core;
    using CitizenFX.Core.Native;
    using Readers;

    public class I18NTest : BaseScript
    {
        public I18NTest()
        {
            I18N.Current
                .SetNotFoundSymbol("$")
                .SetFallbackLocale("en")
                .SetResourcesFolder("i18n")
                .AddLocaleReader(new JsonKvpReader(), ".json")
                .Init(API.GetCurrentResourceName());

            Tick += OnTick;
        }

        private async Task OnTick()
        {
            Debug.WriteLine("test".Translate());
            await Delay(0);
        }
    }
}