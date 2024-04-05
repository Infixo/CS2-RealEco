all: build
BEPINEX_VERSION = 5

clean:
	@dotnet clean

restore:
	@dotnet restore

build-ui:
	@npm install
	@npx esbuild ui_src/ui.jsx --bundle --outfile=dist/ui.js

copy-ui:
	copy dist\commercial.js "C:\Steam\steamapps\common\Cities Skylines II\Cities2_Data\StreamingAssets\~UI~\HookUI\Extensions\panel.realeco.commercial.js"

build: clean restore build-ui
	@dotnet build /p:BepInExVersion=$(BEPINEX_VERSION)

dev-ui:
	@npx esbuild ui_src/ui.jsx --bundle --outfile=dist/ui.js
	copy dist\ui.js "C:\Users\Grzegorz\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\Gooee\Plugins\RealEco.js"

package-win:
	@-mkdir dist
	@cmd /c copy /y "bin\Debug\netstandard2.1\0Harmony.dll" "dist\"
	@cmd /c copy /y "bin\Debug\netstandard2.1\RealEco.dll" "dist\"
	@echo Packaged to dist/

package-unix: build
	@-mkdir dist
	@cp bin/Debug/netstandard2.1/0Harmony.dll dist
	@cp bin/Debug/netstandard2.1/RealEco.dll dist
	@echo Packaged to dist/