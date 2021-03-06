# Salesforce Profile Check
Adjust Salesforce profile or permissionset to another environment.

## Motivation
We ran into problem when deploying profiles between environments. Especially in situation where target environment is lacking some components which are referenced in profile definition. Then you need to repeatedly try validation of changeset to remove all deployment errors (missing components) to move further.

## Purpose
This tool allows users to take complete definition of profile or permissionset from one sandbox and adjust it to another sandbox by removing missing component references.

## Usage
When downloading complete profile definition from one environment, you need to ensure, that it contains all necessary components. I use [force-dev-tool](https://github.com/amtrack/force-dev-tool) with success for that purpose, but be aware of wrong handling of managed components with default settings, see [Notes / Managed components handling by force-dev-tool](#managed-components-handling-by-force-dev-tool).

### Steps
1. Prepare force-dev-tool environments with proper sandboxes ([documentation](https://github.com/amtrack/force-dev-tool#examples))
```sh
$ force-dev-tool remote add DEV user pass-dev --default
$ force-dev-tool remote add INT user pass-int
```
2. Download source environment (DEV) with force-dev-tool using sequence like
```sh
$ force-dev-tool fetch --progress DEV
$ force-dev-tool package -a -f src-dev/package.xml DEV
$ force-dev-tool retrieve -d src-dev DEV
```
3. Download target environment (INT) with force-dev-tool using similar sequence like
```sh
$ force-dev-tool fetch --progress INT
$ force-dev-tool package -a -f src-int/package.xml INT
$ force-dev-tool retrieve -d src-int INT
```
4. Now you have complete metadata from DEV and INT sandboxes to perform profile/permissionset move from DEV to INT sandbox.
5. Delete or backup src-int/profiles and src-int/permissionsets
6. Copy src-dev/profiles and src-dev/permissionsets to src-int subtree, this is needed for SFProfileCheck utility for accessing profiles and permissionsets in context of target sandbox
7. For each profile or permissionset execute profile/permissionset check like:
```sh
$ SFProfileCheck src-int Admin.profile
$ SFProfileCheck src-int "Another Permission.permissionset"
```
8. This command just instructs tool to strip all missing components from profile definition taken from DEV environment to match INT environment. Tool prints out all removed components references so you can check for missing ones easily.
9. You should be able to deploy profile definition to INT sandbox now. There can also appear problem like missing default RecordType or non-modifiable permissions, but it will take fraction of time to fix then manually removing all missing components.

### Notes

#### Managed components handling by force-dev-tool
By default [force-dev-tool](https://github.com/amtrack/force-dev-tool) omits managed package components which is particularly undesirable in our situation as we are using managed components a lot.
Ugly fix is to modify force-dev-tool behaviour by changing [force-dev-tool/lib/fetch-result-parser.js](https://github.com/amtrack/force-dev-tool/blob/master/lib/fetch-result-parser.js) in function FetchResultParser.prototype.getComponents = function(opts). There is a opts.filterManaged variable which controls filtering of managed packages components filtering. You to need to apply following change locally after installation of force-dev-tool.

Original:
```javascript
FetchResultParser.prototype.getComponents = function(opts) {
	var self = this;
	opts = opts ? opts : {};
	opts.filterManaged = opts.filterManaged !== undefined ? opts.filterManaged : true;
```
Target:
```javascript
FetchResultParser.prototype.getComponents = function(opts) {
	var self = this;
	opts = opts ? opts : {};
	opts.filterManaged = opts.filterManaged !== undefined ? opts.filterManaged : false;
```
#### Minimum content in target metadata tree

If you for some reason do not want to download all metadata, but just only a subset, there is a need to keep following directories and their complete content in target metadata tree (in this case src-int/).

This metadata types are evaluated during profile/permissionset checking.

List of needed metadata types:
- applications
- classes
- objects
- layouts
- pages
- tabs

## TODO
 - Optimizations for querying object components (fields and recordtypes). Now it just depends on OS caches when repeatedly opening and working with the same object file.

## License
MIT