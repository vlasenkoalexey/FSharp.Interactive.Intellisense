# FSharp.Interactive.Intellisense

TODO:

+ Build remoting autocomplete service which is started from FSI window and exposing autocomplete methods
+ Autocomplete on System. <- after dot
+ Autocomplete popup control
+ Autocomplete popup control navigation
+ Autocomplete on variable methods (let cool = "blah"; cool. <- after dot)
+ UnitTests for AutocompleteProvider
+ Case insensitive autcomplete
+ Use FSI id on provider registration - it is not possible, using FSI filter to identify if TextView is a correct one
+ More clever registration (on FSI is reloaded or something like that)
+ Why do I see ToString in default completions - fixed
+ Autocomplete for modules doesn't work -> use 
CompilationSourceNameAttribute and FSharpType.IsModule
http://stackoverflow.com/questions/4604139/how-to-get-the-f-name-of-a-module-function-etc-from-quoted-expression-match
- Using unique Remoting channel
+ Auto open default namespaces -> make printfn or Seq. work by default
- Add icons
https://github.com/adamdriscoll/poshtools/blob/85d62a6ec901deba4f86f55483397f039f35090e/PowerShellTools/Intellisense/PowerShellCompletionSource.cs

P2:
- Autocomplete for static variables System.Threading.CurrentThread. <- 
- Autocomplete on Ctrl + tab
- Autocomplete options through interactive
- Try to use internal autocomplete service
	- Try to use internal flag indicating if autocomplete should be enabled
- Figure out how to access instance of FSI.exe process, and use stuff related to SetCompletionFunction
	- Figure out how interactive service is started
	- See if I can find reference through fsi var in console
	- Handle open directive in interactive