# FSharp.Interactive.Intellisense

TODO:

+ Build remoting autocomplete service which is started from FSI window and exposing autocomplete methods

- Autocomplete popup control
- Autocomplete on Ctrl + tab
- Autocomplete on System. <- after dot
- Autocomplete on variable methods (let cool = "blah"; cool. <- after dot)
- Using unique Remoting channel
- More clever registration (on FSI is reloaded or something like that)

- Figure out how to access instance of FSI.exe process, and use stuff related to SetCompletionFunction
	- Figure out how interactive service is started
	- See if I can find reference through fsi var in console
	- Handle open directive in interactive

