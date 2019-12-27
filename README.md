Velixo Code Editor for Acumatica
================================
Prototype of a modern browser-based code editor with IntelliSense support to replace the built-in Acumatica customization code editor. This was originally started as part of a different project which I won't be releasing for now. Based on [Monaco Editor](https://github.com/Microsoft/monaco-editor) and [OmniSharp](https://github.com/OmniSharp).

![Demo](http://g.recordit.co/uBT137T8Cq.gif)

IntelliSense also extends to auto-generated code, like DAC extensions:

![Demo](http://g.recordit.co/zWbciGiA20.gif)

This project demonstrates a few interesting/unusual/undocumented/unsupported customization techniques:
* The use of Autofac and ActivateOnApplicationStart to execute initialization logic when the site starts
* Adding a route and a custom HTTP handler to Acumatica (the editor currently send a HTTP POST command to /editor to handle autocompletion, signature help and type lookup requests from the editor)
* Extracting a command-line application embedded inside the library to a temporary path and using standard input/output to communicate with it
* Using reflection to invoke internal functions

### Prerequisites
* Acumatica 2019 R2 or later (tested with 19.200.0081)
* [.NET Core SDK 3.0 or later](https://www.microsoft.com/net/download/windows) must be installed on the server

Installation
-----------
A complete customization package can be downloaded avaialable from the [releases page](https://github.com/gmichaud/Velixo-AcumaticaCodeEditor/releases)

Contributing
------------
The project has been a learning exercise for me and I don't have much time to invest in it. If you want to contribute, here are some ideas of what could be improved:
* Use websockets instead of HTTP POST calls to communicate with the /editor endpoint
* Remove dependency on .NET Core SDK (or remove dependency on OmniSharp altogether and go straight to Roslyn, like try.dot.net)
* Add support for more features supported by Monaco and OmniSharp like model markers (the red squiggly line that show up when you have an error in your code), editor commands, etc.
* Build a REPL (read-evaluate-print-loop) that uses this editor and can be invoked from any screen in Acumatica

Known Issues
------------
* The customization project editor uses an hardcoded URL for the code editor, which means we have to directly overwrite Pages\SM\SM204580.aspx and Pages\SM\SM204580.aspx.cs; when unpublishing this customization, the file gets deleted, rendering the editor unusable. This can be solved by manually copying the originals from the Files directory or running the Update Site process.
* The first time you trigger auto-complete for a customization project, the server may return with no results; it usually works on the second attempt.
* The lifetime of OmniSharp.exe is managed by a thread which is supposed to kill the process after 10 minutes of inactivity, however if the application domain is restarted it may remain stale.

Support
-----------
This is a prototype only and is provided "as is", with no warranty or support. Use at your own risk!

## Copyright and License

Copyright © `2018` `Velixo`

This component is licensed under the GPLv3 License, a copy of which is available online at https://github.com/gmichaud/Velixo-AcumaticaCodeEditor/blob/master/LICENSE.md
