# Change Log

$$ [1.11.3] - 2022-03-16

### Changed

- Added 'Ignore Line Pattern' option
- Bug Fix. The user setting for 'alignCharacters' was not being read correctly
- Synchronised with VS release

## [1.10.6] - 2021-09-13

### Changed

- Rebuild with updated packages. No other changes.
- Updated readme.md

## [1.10.5] - 2020-07-27

### Changed

- Bug fix. The cache of alignment patterns was not being reset when settings where changed resulting in weird aligning behaviour until VSCode was restarted.
- Added a pattern to the default settings for aligning to ':' characters

## [1.10.4] - 2020-06-08

### Changed

- Bug fix for unalign command not activating the extension
- Command names changed from 'extension.Align' to 'rylogic-textaligner.Align'
- Default keybindings now include '!editorReadonly'

## [1.10.3] - 2020-05-29

### Changed

- Bug fix for alignment based on selected text

## [1.10.0] - 2020-05-28

### Added

- Initial release for VSCode
- Ported from the Visual Studio extension
