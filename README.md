# ASP.NET MVC 5 file provider for the file manager component

This repository contains the ASP.NET MVC 5 file system provider for the Essential JS 2 File Manager component.

## Key Features

A file system provider is an API for access to the physical file system in the FileManager control. It also provides the methods for performing various file actions like creating a new folder, renaming files and deleting files.

ASP.NET MVC file system provider serves the physical file system for the file manager component.

The following actions can be performed with ASP.NET MVC file system provider.

- Read      - Read the files from the local file storage.
- Details   - Gets a file's metadata which consists of Type, Size, Location and Modified date.
- Download  - Download the selected file or folder
- Upload    - Upload's the file. It accepts uploaded media with the following characteristics:
                - Maximum file size:  30MB
                - Accepted Media MIME types: */*
- Create    - Create a new folder.
- Delete    - Delete a folder or file.
- Copy      - Copies the contents of the file from the target location.
- Move      - Paste the copied files to the desired location.
- Rename    - Rename a folder or file.
- Search    - Search a file or folder.

## How to run this application?

To run this application, you need to first clone the `ej2-aspmvc-file-provider` repository and then navigate to its appropriate path where it has been located in your system.

To do so, open the command prompt and run the below commands one after the other.

```
git clone https://github.com/ej2-aspmvc-file-provider ej2-aspmvc-file-provider

cd ej2-aspmvc-file-provider

```

## Running application

Once cloned, open solution file in visual studio.Then build the project and run it after restoring the nuget packages.

## Support

Product support is available for through following mediums.

* Creating incident in Syncfusion [Direct-trac](https://www.syncfusion.com/support/directtrac/incidents?utm_source=npm&utm_campaign=filemanager) support system or [Community forum](https://www.syncfusion.com/forums/essential-js2?utm_source=npm&utm_campaign=filemanager).
* New [GitHub issue](https://github.com/syncfusion/ej2-javascript-ui-controls/issues/new).
* Ask your query in [Stack Overflow](https://stackoverflow.com/?utm_source=npm&utm_campaign=filemanager) with tag `syncfusion` and `ej2`.

## License

Check the license detail [here](https://github.com/syncfusion/ej2-javascript-ui-controls/blob/master/license).

## Changelog

Check the changelog [here](https://github.com/syncfusion/ej2-javascript-ui-controls/blob/master/controls/filemanager/CHANGELOG.md)

© Copyright 2019 Syncfusion, Inc. All Rights Reserved. The Syncfusion Essential Studio license and copyright applies to this distribution.