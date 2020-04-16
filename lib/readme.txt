All projects and sdk projects copy their compiled lib files to this directory
so that other projects only need to refer to this one path.

Also, projects built outside of the 'pr' project tree can copy to this location.

.NET assemblies are not copied here, as NuGet packages are used instead for this purpose.