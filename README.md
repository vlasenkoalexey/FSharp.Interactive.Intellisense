# FSharp.Interactive.Intellisense

TODO:

+ Build remoting autocomplete service which is started from FSI window and exposing autocomplete methods
+ Autocomplete on System. <- after dot
+ Autocomplete popup control
+ Autocomplete popup control navigation
+ Autocomplete on variable methods (let cool = "blah"; cool. <- after dot)
+ UnitTests for AutocompleteProvider
- Use FSI id on provider registration
- Using unique Remoting channel
- More clever registration (on FSI is reloaded or something like that)

P2:
- Autocomplete on Ctrl + tab
- Autocomplete options through interactive
- Add icons
- Try to use internal autocomplete service
	- Try to use internal flag indicating if autocomplete should be enabled
- Figure out how to access instance of FSI.exe process, and use stuff related to SetCompletionFunction
	- Figure out how interactive service is started
	- See if I can find reference through fsi var in console
	- Handle open directive in interactive