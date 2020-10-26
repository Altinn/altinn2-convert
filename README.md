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

### Setting up an empty app
For easy app development, create an empty app in [Altinn Studio](https://altinn.studio) if this does not already exist, and clone this to your local machine.
Files generated using this tool can then be saved directly in the app directory.

## Using the converter
The converter is a command line application. See overview of available commands below.

Run the application from your faviorite IDE (f.ex. VS Code), or from the command line using  `dotnet run`.

### Root command: `extract`

#### SubCommand: `texts`

`extract texts [parameters]`  

Extract texts from infopath form and save them in the Altinn3 format. This command will extract all the available translations that are included in the service package.  

The text files will be saved in the specified ouput directory, under `config/texts`, following Altinn3 app structure:

```
|- my-example-app /
  |- App /
    |- config /
      |- texts /
```

**Required parameters**:

| Parameter (full) | Parameter (short) | Description |
| ---------------- | ----------------- | ----------- |
| `--path`		   | `-p`			   | The full path to the InfoPath zip-file contianing the service files |
| `--outputPath`   | `-o`			   | The full path to the Altinn 3 app where the texts should be saved. F.ex. `C:/Repos/Apps/my-example-app/App` |


#### SubCommand: `layout`

`extract layout [parameters]`   

Extract form layout from infopath form and save them in the Altinn3 format. Fields are automatically connected to corresponding field in the data model. 
This command will attempt to connect form components with corresponding texts from the form where possible. 
When this is not possible (due to f.ex. unclear/ambiguous structure of infopath file), the form components data model binding will be used as text instead. 
Texts that are not possible to connect to a form component (f.ex. headings, descriptions, or when connection to form component is ambiguous) are added as text (paragraph) components in the layout.

** Note:** For texts to appear when running the Altinn 3 app, the `texts` subcommand must also be run.


This command supports the new [multi-page forms functionality](https://altinn.github.io/docs/altinn-studio/app-creation/ui-editor/multiple-pages/) that is available for the apps. This means that if the infopath form has multiple pages, then the generated Altinn 3 form layout
will have the same pages. Component types from the infopath form are mapped to component types in Altinn 3. Currently the following Altinn 3 component types
are supported:

- Input
- TextArea
- RadioButton
- Dropdown

Support for more components will be added as this tool is developed further. 

The layout file(s) will be saved in the specified ouput directory, under `ui/layouts`, following Altinn3 app structure:

```
|- my-example-app /
  |- App /
    |- ui /
      |- layouts /
```

**Required parameters**:

| Parameter (full) | Parameter (short) | Description |
| ---------------- | ----------------- | ----------- |
| `--path`		   | `-p`			   | The full path to the InfoPath zip-file contianing the service files |
| `--outputPath`   | `-o`			   | The full path to the Altinn 3 app where the layouts should be saved. F.ex. `C:/Repos/Apps/my-example-app/App` |

### Known issues
As this project is currently at the proof of concept stage, there are some known issues/missing functionality. 

#### Order of components in the layout
The order of the components in the layout might not be exactly as expected from the infopath form. The order of fields within a group will most likely be correct, but the 
groups of components might be ordered differently than from the infopath file.

This can be solved in the Altinn 3 app by re-ordering the components, either using the UI editor in Altinn Studio, or manually in the layout file(s).