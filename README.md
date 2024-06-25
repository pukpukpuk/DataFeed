<p align="center"><img src="https://github.com/pukpukpuk/DataFeed/assets/83503177/b98d3030-3d78-4926-8c62-0b4610270df5" width="500" ></p>

**DataFeed** is a reimagining of the standard Unity console. 

The functionality of DataFeed is divided into two windows:
1. **Log viewer window**
2. **Command input window** (Auxiliary to the first and optional)

<h2 align="center">Window Features</h2>
<h3>Log Viewer Window</h3>

Logs are displayed in a practical table format
> <img width="400" src="https://github.com/pukpukpuk/DataFeed/assets/83503177/df1ca917-8270-4307-a7c7-cc82c796d412">

Support for customizable layers and tags for logs and filtering entries by them
> <img width="400" src="https://github.com/pukpukpuk/DataFeed/assets/83503177/bc5f2a7e-e50b-4a1a-a4a4-d30c89f2605d">&emsp;&ensp;<img width="250" src="https://github.com/pukpukpuk/DataFeed/assets/83503177/9c6de8f4-d5db-4cd7-9455-b9278e102820">

Auxiliary optional entries:
* Entry indicating a significant time difference between entries (useful for turn-based games)
> <img width="350" src="https://github.com/pukpukpuk/DataFeed/assets/83503177/92f9edac-bae8-4d77-b134-3282e3b5fb30">
* Entry combining non-passed filter entries into a collapsible group
> <img width="350" src="https://github.com/pukpukpuk/DataFeed/assets/83503177/7e83d297-6037-4b65-b7ae-e33d45359b3a">

<h3>Command Input Window</h3>

In addition to the command input line, it has a completion list. The list consists of two parts:

1. **Left** - displays completions based on existing commands and their arguments.
2. **Right** - displays a list of suitable commands based on previously entered commands.

> <img width="450" src="https://github.com/pukpukpuk/DataFeed/assets/83503177/4f52193f-3c19-45bc-bbfc-d55494ad9185">

In addition to mouse clicks, the list fully supports keyboard control:

1. `Shift+U` - **Focus on input window**
2. `Tab` - **Toggle focus between input line and list**
3. `Arrows` - **Move through the completion list**
4. `uhjk` - **Alternative to `Arrows`**. You can change these keys in the config

<h2 align="center">Usage Guide</h2>

<h3>Config path</h3>

`Assets/Plugins/Pukpukpuk/DataFeed/Resources/DataFeed/`

<h3>Sending messages to console</h3>

`DebugUtils` is responsible for console message output:

```cs
DebugUtils.Log("Hello World!");
DebugUtils.LogLayer("Hello World!", "Game", tag:"SomeTag");
```

<h3>How to make your own commands</h3>

Each command must inherit the `Pukpukpuk.DataFeed.Input.Command` class, and there is no need to register the command additionally.

The operation of the command is specified in the `Execute_hided()` method, and the list of completions in `GetCompletions()`. Other information is specified in the xml-comments of the `Command` class.
