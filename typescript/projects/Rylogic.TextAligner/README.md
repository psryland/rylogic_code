# Rylogic Text Aligner

_Rylogic Text Aligner is also available for [Visual Studio 2019 and earlier](https://marketplace.visualstudio.com/items?itemName=Rylogic.RylogicTextAligner) and [Visual Studio 2022 and above](https://marketplace.visualstudio.com/items?itemName=Rylogic.RylogicTextAligner64)_

## Overview

Rylogic Text Aligner is an extension that adds two commands to the command palette for vertically aligning text: _Align_ and _Unalign_. Vertical text alignment is a powerful productivity aid when used in combination with column selection. Vertically aligned text also leverages your subconscious ability to spot patterns, making pattern-breaking bugs much easier to spot. Notice how much easier it is to spot the bug in the second of the following two code examples:

![unaligned_bug](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/unaligned_bug.png "Un-aligned code")
![aligned_bug](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/aligned_bug.png "Un-aligned code")

By default, Rylogic Text Aligner uses the following keyboard bindings:

* Ctrl + Alt + ] = Vertically align text
* Ctrl + Alt + [ = Unalign text

To change these, search for 'Align' and 'Unalign' in the Keyboard Shortcuts editor (File → Preferences → Keyboard Shortcuts).

Rylogic Text Aligner is highly configurable. After reading though these simple examples, check out the [configuration](#Configuration) options.

## Align Command

The first, and most basic use case, is to align assignments:

![unaligned](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/unaligned.png "Un-aligned code")
![aligned](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/aligned.png "Un-aligned code")

To align some text, move the caret onto a line of text within a block and hit your keyboard shortcut. The extension intelligently searches above and below the current caret position, identifies the alignment group nearest the caret and aligns the text. Note that alignment is not limited to just assignments, repeatedly pressing your keyboard shortcut will identify other alignment groups and align to those. To demostrate this, create a new text file and copy in the following text:

```txt
apple = red, 32;
banana = yellow, 512;
carrot = orange, 256;
cucumber = green, 16;
pea = green, 8;
```

Now, move the caret to the position just after the 'r' in 'cucumber' and press your keyboard shortcut twice. The text should be aligned as follows:

![usage1](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/usage1.png "Alignment example")
![usage2](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/usage2.png "Alignment example")
![usage3](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/usage3.png "Alignment example")

You can also tell the extension to align to specific characters by selecting the text to align to before pressing your keyboard shortcut. For example, select a ';' character then align:

![usage4](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/usage4.png "Aligning to a selection example")
![usage5](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/usage5.png "Aligning to a selection example")

Sometimes there is a need to limit the range of lines that aligning is applied to. This can be achieved by selecting multiple lines before hitting align:

![usage6](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/usage6.png "Limiting to selected lines example")
![usage7](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/usage7.png "Limiting to selected lines example")

Notice that whole lines do not need to be selected.

## Unalign Command

The _Unalign_ command uses logic similar to _Align_ to select the appropriate alignment group and range of lines to operate on. _Unalign_ removes consecutive whitespace to the left of the matched pattern on each line. For alignment groups with leading space, a single whitespace character is added. Trailing whitespace is also removed for affected lines.

Using the text example from above, unaligning should result in the following sequence. Notice that the priority of alignment groups is reversed, compared to _Align_, so that _Unalign_ is almost the inverse operation:

![unalign_usage1](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/unalign_usage1.png "Unalignment example")
![unalign_usage2](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/unalign_usage2.png "Unalignment example")
![unalign_usage3](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/unalign_usage3.png "Unalignment example")

Similarly, selecting text to unalign on is also possible:

![unalign_usage4](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/unalign_usage4.png "Unalignment example")
![unalign_usage5](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/unalign_usage5.png "Unalignment example")

The range that unaligning is applied to can be limited by multi-line selection. As before, whole lines do not need to be selected:

![unalign_usage6](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/unalign_usage6.png "Limiting unalignment example")
![unalign_usage7](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/unalign_usage7.png "Limiting unalignment example")

Unlike _Align_, the _Unalign_ command can also be used with selected text where no alignment groups match. In this case, the _Unalign_ command replaces any consecutive white-space with single white-space characters. The command preserves leading indentation, and is aware of C-style literal strings, including multi-line strings so long as they are spanned by the selection.

![non_pattern_unalign1](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/non_pattern_unalign1.png "Non-pattern unalignment example")
![non_pattern_unalign2](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/non_pattern_unalign2.png "Non-pattern unalignment example")

Notice that multiple whitespace within the quoted literal string is preserved.

## Configuration

***Search for TextAligner in Settings***

#### Tabs vs. Spaces

Before going any further, Rylogic Text Aligner allows the type of whitespace used for aligning to be configured. You can use spaces, tabs, or a even a mixture (weirdos!) by setting the _Align Characters_ option. The supported schemes are:

* Spaces - space characters are used, aligning to the left-most common column.
* Tabs - tab characters are used, aligning to the left-most common tab boundary based on the user's tab size setting.
* Mixed - both tab and space characters are used, aligning to the left-most common column using tabs were possible.

#### Alignment Groups and Patterns

Almost any classification of text tokens can be aligned by making use of _Alignment Groups_ and _Alignment Patterns_. The following image is borrowed from the Visual Studio version of the extension to help illustrate how the settings are organised.

![options.png](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/options.png "Alignment options")

The character sequences recognised as 'alignable' are called _Alignment Groups_, where each group contains one or more _Alignment Patterns_. The order of groups influences the priority of group selection. The patterns within a group are all considered equivalent.

For example, in the image above, the 'Assignments' group is the highest priority and consists of three patterns that are all considered assignments; ```=```, ```+=, -=, *=, /=, %=, ^=, ~=, &=, |=```, and ```&&=, ||=```.

![assignment_align_example.gif](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/assignment_align_example.gif "assignment align example")

Each _Alignment Group_ has a _Leading Space_ value that is the minimum number of whitespace columns before any aligned text. In the example above, there is one column space between the end of 'assignments' and the '&&=' text.

Each _Alignment Pattern_ has an _Offset_ and a _Minimum Width_. The offset controls the horizontal alignment of patterns within the _Alignment Group_. The matched text for each pattern is padded with whitespace, up to the pattern's minimum width.

#### Adding your own Groups and Patterns

In settings, search for 'TextAligner' and look for 'Groups', then edit the JSON data representation of the alignment groups and patterns. It's a good idea to use an online Regex testing tool to design and test your regular expressions (for example: [regexr.com](https://regexr.com/)). Note however that VSCode uses ES2018 regular expressions with support for negative lookbehind and other features, but many online testers do not.

#### Ignore Line Pattern

The ignore line pattern can be used to make certain lines invisible to the aligning algorithm. This can be useful for aligning definitions that are separated by blank lines and comments.

For example, if the _Ignore Line Pattern_ is set to the regular expression pattern ```(^\s*$)|(^\s*//)``` the aligner will ignore all blank lines and lines starting with a single line comment.

![ignored_lines.gif](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/ignored_lines.gif "ignored lines example")

#### Support & Donate

If you like Rylogic Text Aligner and would like to say thanks, a donation would be greatly appreciated.

[![paypal donate](https://raw.githubusercontent.com/psryland/rylogic_code/master/typescript/projects/Rylogic.TextAligner/images/paypal_donate_logo.png)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=accounts%40rylogic.co.nz&lc=NZ&item_name=Donation%20for%20Rylogic.TextAligner&currency_code=NZD&bn=PP%2dDonationsBF)

Bugs should be reported to support@rylogic.co.nz

#### Version History

see: [ChangeLog](https://marketplace.visualstudio.com/items/Rylogic.rylogic-textaligner-vscode/changelog)
