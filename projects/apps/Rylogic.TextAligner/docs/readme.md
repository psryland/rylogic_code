# Rylogic Text Aligner

_This version is for Visual Studio 2022 and future versions. For Visual Studio 2019 and earlier, use this version [Rylogic.TextAligner for Visual Studio 2019](https://marketplace.visualstudio.com/items?itemName=Rylogic.RylogicTextAligner)_

_Rylogic Text Aligner is now also available for [Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=Rylogic.rylogic-textaligner-vscode)_!

## Overview

Rylogic Text Aligner is an extension that adds two commands to the edit menu for vertically aligning text: _Align_ and _Unalign_. Vertical text alignment is a powerful productivity aid when used in combination with column selection. Vertically aligned text also leverages your subconscious ability to spot patterns, making pattern-breaking bugs much easier to spot. Notice how much easier it is to spot the bug in the second of the following two code examples:

![unaligned_bug](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/unaligned_bug.png "Un-aligned code")
![aligned_bug](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/aligned_bug.png "Un-aligned code")

The _Align_ and _Unalign_ commands are available under the Edit -> Advanced Menu. For convenience however, I recommend creating keyboard shortcuts. Select _Options_ from the _Tools_ menu, and then _Keyboard_ under the _Environment_ section. Type "Edit.Align" into the search text field, then choose your keyboard shortcut, and click assign. Repeat the process for "Edit.Unalign". I prefer the shortcuts:

```txt
Align   = Ctrl+Alt+]
Unalign = Ctrl+Alt+[
```

but the choice is up to you.

![shortcut_setup](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/keyboard_shortcut.png "Creating a keyboard shortcut for Align")

Rylogic Text Aligner is highly configurable. After reading though these simple examples, check out the [configuration](#Configuration) options.

## Align Command

The first, and most basic use case, is to align assignments:

![unaligned](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/unaligned.png "Un-aligned code")
![aligned](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/aligned.png "Un-aligned code")

To align some text, move the caret onto a line of text within a block and hit your keyboard shortcut. The extension intelligently searches above and below the current caret position, identifies the alignment group nearest the caret and aligns the text. Note that alignment is not limited to just assignments, repeatedly pressing your keyboard shortcut will identify other alignment groups and align to those. To demonstrate this, create a new text file and copy in the following text:

```txt
apple = red, 32;
banana = yellow, 512;
carrot = orange, 256;
cucumber = green, 16;
pea = green, 8;
```

Now, move the caret to the position just after the 'r' in 'cucumber' and press your keyboard shortcut twice. The text should be aligned as follows:

![usage1](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/usage1.png "Alignment example")
![usage2](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/usage2.png "Alignment example")
![usage3](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/usage3.png "Alignment example")

You can also tell the extension to align to specific characters by selecting the text to align to before pressing your keyboard shortcut. For example, select a ';' character then align:

![usage4](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/usage4.png "Aligning to a selection example")
![usage5](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/usage5.png "Aligning to a selection example")

Sometimes there is a need to limit the range of lines that aligning is applied to. This can be achieved by selecting multiple lines before hitting align:

![usage6](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/usage6.png "Limiting to selected lines example")
![usage7](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/usage7.png "Limiting to selected lines example")

Notice that whole lines do not need to be selected.

## Unalign Command

The _Unalign_ command uses logic similar to _Align_ to select the appropriate alignment group and range of lines to operate on. _Unalign_ removes consecutive whitespace to the left of the matched pattern on each line. For alignment groups with leading space, a single whitespace character is added. Trailing whitespace is also removed for affected lines.

Using the text example from above, unaligning should result in the following sequence. Notice that the priority of alignment groups is reversed, compared to _Align_, so that _Unalign_ is almost the inverse operation:

![unalign_usage1](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/unalign_usage1.png "Unalignment example")
![unalign_usage2](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/unalign_usage2.png "Unalignment example")
![unalign_usage3](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/unalign_usage3.png "Unalignment example")

Similarly, selecting text to unalign on is also possible:

![unalign_usage4](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/unalign_usage4.png "Unalignment example")
![unalign_usage5](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/unalign_usage5.png "Unalignment example")

The range that unaligning is applied to can be limited by multi-line selection. As before, whole lines do not need to be selected:

![unalign_usage6](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/unalign_usage6.png "Limiting unalignment example")
![unalign_usage7](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/unalign_usage7.png "Limiting unalignment example")

Unlike _Align_, the _Unalign_ command can also be used with selected text where no alignment patterns match. In this case, the _Unalign_ command replaces any consecutive white-space with single white-space characters. The command preserves leading indentation, and is aware of C-style literal strings, including multi-line strings so long as they are spanned by the selection.

![non_pattern_unalign1](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/non_pattern_unalign1.png "Non-pattern unalignment example")
![non_pattern_unalign2](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/non_pattern_unalign2.png "Non-pattern unalignment example")

Notice that multiple whitespace within the quoted literal string is preserved.

## Configuration

The character sequences recognised as 'alignable' are defined in the options. Select _Options_ from the _Tools_ menu, then _Align Options_ under the _Rylogic_ section.

![options.png](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/options.png "Alignment options")

All options are saved to an XML file in your AppData folder:
```%USERPROFILE%\AppData\Roaming\Rylogic\VSExtension\align_patterns.xml```

#### Tabs vs. Spaces

The _Alignment Characters_ option is used to select between different whitespace styles. The supported schemes are:

* Spaces - space characters are used, aligning to the left-most common column.
* Tabs - tab characters are used, aligning to the left-most common tab boundary based on the user's tab size setting,
* Mixed - both tab and space characters are used, aligning to the left-most common column using tabs were possible,

#### Alignment Groups and Patterns

The top panel lists the _Alignment Groups_, and the lower panel contains the _Alignment Patterns_ for the selected group. The order of groups influences the priority when selecting a group for aligning. The patterns within each group are all considered equivalent.

For example, in the image above, the 'Assignments' group is the highest priority and consists of three patterns that are all considered assignments; ```=```, ```+=, -=, *=, /=, %=, ^=, ~=, &=, |=```, and ```&&=, ||=```. Patterns can be simple substrings, wildcard expressions, or regular expressions.

![assignment_align_example.gif](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/assignment_align_example.gif "assignment align example")

Each _Alignment Group_ has a _Leading Space_ value that is the minimum number of whitespace columns before any aligned text. In the example above, there is one column space between the end of 'assignments' and the '&&=' text.

Each _Alignment Pattern_ has an _Offset_ and a _Minimum Width_. The offset controls the horizontal alignment of patterns within the _Alignment Group_. The matched text for each pattern is padded with whitespace, up to the pattern's minimum width.

To edit and test a pattern, select the pencil icon to display the pattern editor:

![pattern_editor.png](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/pattern_editor.png "Edit a pattern")

#### Ignore Line Pattern

The ignore line pattern can be used to make certain lines invisible to the aligning algorithm. This can be useful for aligning definitions that are separated by blank lines and comments.

For example, if the _Ignore Line Pattern_ is set to the regular expression pattern ```(^\s*$)|(^\s*//)``` the aligner will ignore all blank lines and lines starting with a single line comment.

![ignored_lines.gif](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/ignored_lines.gif "ignored lines example")

#### More Example Alignment Patterns

* Single Line C Macros

```regex
(?<=#define\s+[_A-Za-z][_A-Za-z0-9]*\(.*\)\s+)(\b.*)
(?<=#define\s+[_A-Za-z][_A-Za-z0-9]*\s+)(\b.*)
```
  
#### Support & Donate

If you like Rylogic Text Aligner and would like to say thanks, a donation would be greatly appreciated.

[![paypal donate](https://raw.githubusercontent.com/psryland/rylogic_code/master/projects/apps/Rylogic.TextAligner/docs/paypal_donate_logo.png)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=accounts%40rylogic.co.nz&lc=NZ&item_name=Donation%20for%20Rylogic.TextAligner&currency_code=NZD&bn=PP%2dDonationsBF)

Bugs should be reported to support@rylogic.co.nz

#### Version History

* *v1.11.7 - 2022-06-30*
  * Migrated the VS2022 version of the extension to use AsyncPackage (VS2019 is still based on 'Package' for backward compatibility to VS2013)
  * Fix for an exception when using 'Unalign' on the last line of a file.
  * Added a simple log to capture errors and better exception handling.

* *v1.11.6 - 2022-05-05*
  * Changed the behaviour of unaligning. Previously, unalign would apply patterns in reverse priority order so that unaligning was the mirror of aligning. However, I think this behaviour is unintuitive, because it seems like the priority order is not being used. Now, unaligning uses the same priority as aligning, so patterns next to the caret will unalign first, as would be expected.

* *v1.11.5 - 2022-04-14*
  * Bug Fix. Settings synchronised across multiple running instances of Visual Studio (including different versions)
  * Bug Fix. Line ignore pattern was not being preserved in settings.
  
* *v1.11.4 - 2022-03-30*
  * Bug Fix. The pattern for matching member variables that end in '_' was incorrectly matching substrings.
  * Changed the default 'Colon' matching pattern to only match single colons, not '::'.
  
* *v1.11.3 - 2022-03-16*
  * Added _Ignore Line Pattern_

* *v1.11.2 - 2022-01-30*
  * Bug fix

* *v1.11.1 - 2022-01-27*
  * Updated to support Visual Studio 2022

* *v1.10.0 - 2020-05-29*
  * Synchronised code with the Visual Studio Code implementation
  * Minor bug fixes

* *v1.9 - 2020-05-27*
  * Unalign feature added.
  * Default leading space for 'Commas' group set to 0. Set it back to 1 if you prefer space to the left of your commas.

* *v1.8 - 2020-03-23*
  * Option to use spaces, tabs, or both for aligning added.
  * Replaced the UI components, used in the options dialog, with an implementation based on WPF. This solved a bug related to RichEdit controls and LoadLibrary in the previous version (thanks Quinten!).
  
* *v1.7 - 2018-07-02*
  * First public release.
