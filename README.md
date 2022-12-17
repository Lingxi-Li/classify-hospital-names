# Classify Hospital Names

# The Problem
Multiple name strings that do not exactly match may refer to the same hospital for the following reasons.
* The strings refer to former names and aliases of the hospital.
* The strings follow different conventions, e.g., half/full-width characters, "`0..9`"/"`零..九`", simplified/traditional Chinese characters.
* The strings are structured differently and with different syntax, e.g.,
  - the string may be a single name,
  - the string may encode multiple aliases using a certain syntax,
  - the string may encode annotations using a certain syntax.
* The strings may have typos.

## Classification Problem
Given two lists of strings: 1) `names` (external names) and 2) `labels` (internal names), the task is to label each string in `names` with a string in `labels` such that the two refer to the same hospital, or none if no match is found.

## Clustering Problem
Given a list of strings `names` (external names), the task is to cluster strings that refer to the same hospital and output the most informative string in each cluster.

# Tool Usage
The command line tool `Label.exe` operates in either classification or clustering mode. In either mode, output file would be UTF-8 encoded.

## Classification Mode

~~~
Label.exe <names> <labels> <output>
~~~

|Parameter|Description|
|---|---|
|`names`|Path to the file containing the strings to be labeled, each in a separate line.|
|`labels`|Path to the file containing the label strings, each in a separate line.|
|`output`|Path to the file to write result to. The output is a two-column table with each row in a separate line and each column being tab separated. First column exactly matches `names` in order. Second column contains corresponding label from `labels` or an empty string if no match is found.|

## Clustering Mode

~~~
Label.exe <names> <output>
~~~

|Parameter|Description|
|---|---|
|`names`|Path to the file containing the strings to be clustered, each in a separate line.|
|`output`|Path to the file to write result to. The output will contain the most informative string for each cluster, each in a separate line.|

## GUI
If the tool is invoked with incorrect input, a GUI dialog will show up to help collect input. When `LabelsFilePath` is specified, the tool runs in classification mode and otherwise in clustering mode.

Classification mode supports concurrency and the tool runs with `max(logical processor count - 1, 1)` thread(s) by default. Concurrency can only be tuned with GUI. Clustering mode does not support concurrency and always runs single threaded.
