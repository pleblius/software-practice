README
Authors: Alexa Fresz, Tyler Wilcox

WARNING: FOR SOME REASON, THE CONTENT TEXT BOX IS NEARLY UNREADABLE IN DARK MODE ON WINDOWS. IF YOU CAN'T SEE IT, PLEASE USE LIGHT MODE ON WINDOWS. THANK YOU

EXTERNAL CODE RESOURCES
MAUI Community Toolkit (https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/), used for saving files.

IMPLEMENTATION NOTES
We have a custom changed object that our extra features use for copying and undoing. Keeps track of the name, the new contents, and the old contents of that object. We use stacks for our redo and undo functions. Redo stack is cleared when a change is made. 
Exceptions are caught in a generic try-catch block that reads the type of the exception in order to display what went wrong in a DisplayAlert box. 
File saving is done using the Community Toolkit, which lets us add some extra features such as the Save button. 
For warning about overwrites, we simply use a DisplayAlert which has two options that returns a bool, and if the user presses no, we just abort whatever action was going to overwrite. 
Copying a cell (through Copy or Cut) checks to make sure we've copied/pasted before. 
You can paste a copied cell even if you've created a new spreadsheet. This is intended as to better represent how an actual clipboard works. 
A new or opened file removes all redo/undos in the previous file.
A SavePath is always stored after any save. It is blank if nothing has been saved, and as such requires you to choose a name before being able to use the normal save button. Opening a file preserves filepath so you can save easily. A new file overwrites the save path.


CHANGES LOG
07/10/2023: Began work on PS6. README created, design initialized. Kept most of the files we were given and edited them directly. Most extra features decided on. 
15/10/2023: Worked on adding all of our buttons to the GUI: Name/Value/Contents, Save button (In the file drop out). Spreadsheet now somewhat works. 
21/10/2023: Copy, cut, paste, undo, redo functions added.
22/10/2023: Worked on saving and loading. Fixed errors with added functions. All saving and loading works, with overwrites protected. Spreadsheet feature complete with all found crashes fixed. Most of the README filled out with the Help, External Code Resources, Extra Features, and Implementation Notes sections. 

EXTRA FEATURES
You can copy, cut, and paste cells from the Edit button. You must be selecting the cell you want to do this to. 
If a paste causes an error, standard error handling applies. 
You can undo and redo your changes. Making a new change overwrites any redos you may have. 
You can Save As for any file, if you haven't saved before on a new spreadsheet you will be prompted to choose a file. 
After a file is saved, you can use the save button instead of manually picking the same file each time, which is significantly quicker.
Interrupting file saving or opening won't crash the application.
FormulaErrors display their reason instead of a generic FormulaError message (Ie, DIV/0 or #REF)

HELP
Selection
Clicking on any cell allows you to select it. When selected, a cell shows it's name and value below the menu bar. A1 is the default selection.

Setting Contents
The contents box allows you to set the contents of the cell. You can input any number or text you want by typing into the contents box, then pressing enter. Formulas are inputted by putting an equals sign at the beginning (eg, =a1+3). Cell names can be upper or lowercase.

New File
File -> New lets you start a new spreadsheet. Warns you if you're going to lose data in doing so, and allows you to reverse your decision.

Open File
File -> Open lets you open a spreadsheet from a file. Won't let you open files that aren't spreadsheets, and warns you if you're going to overwrite data. Nothing happens if you open files that aren't spreadsheets.

Saving File 
File -> Save OR File -> Save As allows you to save your spreadsheet. Save will prompt you to Save As if you haven't saved before. EXTRA FEATURE: You can save an already saved spreadsheet by pressing File -> Save, and will not be prompted.

Overwriting File
If any action you take when opening or creating a new spreadsheet would overwrite work, then you are warned of this loss of data. If you try and close the application without saving, only god can help you. Sorry.

Undo, Redo
EXTRA FEATURE. You can undo and redo your changes. If you undo, then make a change, your redos are lost, as is standard behavior. You can undo as many changes as you've made.

Cut, Copy, Paste
EXTRA FEATURE. You can use the Edit menu option to cut, copy, and paste. Edit -> Copy allows you to copy the contents of a cell. You can then use Edit -> Paste to paste the contents into another cell. Catches any exceptions, eg pasting (=b1+2) into B1 will not change the spreadsheet. Edit -> Cut removes the contents of a cell when pressed, and allows you to paste normally. All values are updated when code is cut.

Dark Mode
Dark mode on Windows makes the text in the contents box nearly unreadable. Please use light mode if you're having this issue. Sorry.