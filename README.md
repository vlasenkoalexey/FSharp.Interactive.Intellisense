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
+ Auto open default namespaces -> make printfn or Seq. work by default
+ Add icons
https://github.com/adamdriscoll/poshtools/blob/85d62a6ec901deba4f86f55483397f039f35090e/PowerShellTools/Intellisense/PowerShellCompletionSource.cs
+ TODO: wrap fsiToolWindow into class -> wrapped into method
+ Using unique Remoting channel
+ Replaced remoting with WCF named pipes
+ Retry accessing client autocomplete service after registration to make sure that it is up and running
+ Find a way to access FSI autocomplete API - done !!!!
+ No autocomplete on space - dismiss session
+ Merge output of custom built fsi provider and FSI autocomplete service
+ Pass instance of ICompletion interface from server
+ Autocomplete on Ctrl + tab
+ Autocomplete options

P2:
- Autocomplete for static variables let somevar = Some(1);; somvar.Value. <- 
  - Need to evaluate something in FSI -> try using redirected input
- Show completion tooltips
- See if it is possible to integrate with https://github.com/fsharp/FsAutoComplete