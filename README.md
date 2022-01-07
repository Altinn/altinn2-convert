# altinn2-convert
Command line tool for converting Altinn 2 reporting services into Altinn 3 apps.

**Note:** This tool is currently a proof of concept (POC), and functionality is therefore limited.

## Getting started
These instructions will get you a copy of the project up and running on your local machine. 

### Installing
Clone the [altinn2-convert repo](https://github.com/Altinn/altinn2-convert) and navigate to the folder.

```
git clone https://github.com/Altinn/altinn2-convert
cd altinn2-convert
```
Note that you need to have a checked out copy of `altinn-studio` in the same root folder, so we can access app template and shared libraries.
You also need to build the DataModelling project for the reference to work
```
dotnet build src/studio/src/designer/DataModeling/DataModeling.csproj
```

### Setting up an empty app
For easy app development, create an empty app in [Altinn Studio](https://altinn.studio) if this does not already exist, and clone this to your local machine.
Files generated using this tool can then be saved directly in the app directory.

## Using the converter

Run the application from your faviorite IDE (f.ex. VS Code), or from the command line using  `dotnet run`. Modify the `mode` variable in `Program.cs` to run in different modes

- "generate": Generate models for the Altinn3 json files based on json schema on altinncdn.no
- "test": Convert a single schema from `TULPACKAGE.zip` in the root directory to `out/` in the project root
- "run": Convert all pacages in "~/TUL" to a Altinn3 app in `~/TULtoAltinn3`.

There might be other modes in the future when the project matures.

