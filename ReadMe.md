# UmbracoFileSystemProviders.RackspaceCloudFiles

This repository contains an implementation of an Umbraco file system provider for Rackspace Cloud Files, used to offload static files in the media section to this cloud provider.

## To Use

Install from NuGet with:

```
PM> Install-Package UmbracoFileSystemProviders.RackspaceCloudFiles
```

## Status

Intial 0.1 release primarily for the needs of a single project, currently in development.  Not tested in production yet.

## Thanks

- To [Prosell](http://prosell.com/) for supporting the development of this provider and agreeing to open-source it
- Elijah Glover and James Jackson-South for writing and open-sourcing the implementations that target [Amazon S3](https://github.com/ElijahGlover/Umbraco-S3-Provider) and [Azure Storge](https://github.com/JimBobSquarePants/UmbracoFileSystemProviders.Azure) respectively, that were heavily referenced in writing this provider

## Version history

- 0.1.0
    - Initial release
