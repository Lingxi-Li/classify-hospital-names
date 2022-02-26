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

Given two lists of strings: 1) `names` (external names) and 2) `labels` (internal names), the task is to label each string in `names` with a string in `labels` such that the two refer to the same hospital, or none if no match is found.

# Tool Usage
The command line tool `Label.exe` is used as below.

~~~
Label.exe <names> <labels> <output>
~~~

|Parameter|Description|
|---|---|
|`names`|Path to the file containing the strings to be labeled, each in a separate line.|
|`labels`|Path to the file containing the label strings, each in a separate line.|
|`output`|Path to the file to write result to. The output is a two-column table with each row in a separate line and each column being tab separated. First column exactly matches `names` in order. Second column contains corresponding label from `labels` or an empty string if no match is found.|

All three files are UTF-8 encoded.

The tool processes strings in `names` in order and logs to console the current string being processed. **For correct display of string contents, use a console font that supports Chinese characters.** In the end, the tool logs a summary to console.
