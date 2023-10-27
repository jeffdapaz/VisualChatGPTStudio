# Visual chatGPT Studio

English | [Chinese](https://github.com/jeffdapaz/VisualChatGPTStudio/blob/master/README-zh.md)

- For Visual Studio 2022: [here](https://marketplace.visualstudio.com/items?itemName=jefferson-pires.VisualChatGPTStudio)
- For Visual Studio 2019: [here](https://marketplace.visualstudio.com/items?itemName=jefferson-pires.VisualChatGPTStudio2019)

## Description

This is an extension that adds chatGPT functionality directly within Visual Studio.

You will be able to consult the chatGPT directly through the text editor or through a new specifics tool windows.

Watch here some examples:

[<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/275614252-5f824ba6-df13-45e3-928e-086609fe1bcd.png" width="70%">](https://youtu.be/eU5hEh6e5Ow)

## Features in text editor

Select a method and right click on text editor and you see these new chatGPT commands:

![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/2.1.0/1697062741546/image__2.png)

- **Complete:** Start write a method, select it and ask for complete.
- **Add Tests:** Create unit tests for the selected method.
- **Find Bugs:** Find bugs for the selected code.
- **Optimize:** Optimize the selected code.
- **Optimize (Diff View):** Optimize the selected code, however, instead of the result being written in the code editor, a new window will open where you can compare the original code with the version optimized by chatGPT.
- **Explain:** Write an explanation of the selected code.
- **Add Comments:** Add comments for the selected code.
- **Add Summary:** Add Summary for C# methods.
- **Add Summary For Entire Class:** Add Summary for entire C# class (for methods, properties, enums, interfaces, classes, etc). Don't need to select the code, only run the command to start the process.
- **Ask Anything:** Write a question on the code editor and wait for an answer.
- **Translate:** Replace selected text with the translated version. In Options window edit the command if you want translate to another language instead English.
- **Custom Before:** Create a custom command through the options that inserts the response before the selected code.
- **Custom After:** Create a custom command through the options that inserts the response after the selected code.
- **Custom Replace:** Create a custom command through the options that replace the selected text with the response.
- **Cancel:** Cancel receiving/waiting any command requests.

And if you desire that the responses be written on tool window instead on the code editor, press and hold the SHIFT key and select the command (not work with the shortcuts).

If you want chatGPT to respond in another language and/or want to customize the commands for some reason, you can edit the default commands through the options:

<img src="https://user-images.githubusercontent.com/63928228/226494626-d422a843-2512-4dee-a177-045f39c0b6d3.png" width="70%">

For example, by changing the "Explain" prompt of the Explain command to "Explicar en español" (or just "Explicar"), the OpenAI API will write the comments in Spanish instead of using the default English command.

## Features by "Visual chatGPT Studio" tool window

In this tool window you can ask questions to chatGPT and receive answers directly in it.

![image](https://user-images.githubusercontent.com/63928228/225486306-d29b1ec3-2ccd-4d74-8153-806a84abe5ea.png)

This window can also used to redirect the commands responses to it holding the SHIFT key while execute some command, to avoid edit the code when you do not want.

You will find this window in menu View -> Other Windows -> Visual chatGPT Studio.

## Features by "Visual chatGPT Studio Turbo" tool window

In this window editor you can interact directly with chatGPT as if you were in the chatGPT portal itself:

Unlike the previous window, in this one the AI "remembers" the entire conversation:

![image](https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/ff47bf5c-8324-46ba-a039-173c172337e0)

You can also interact with the opened code editor through the "Send Code" button. Using this button the OpenAI API becomes aware of all the code in the opened editor, and you can request interactions directly to your code, for example:

- Ask to add new method on specific line, or between two existing methods;
- Change a existing method to add a new parameter;
- Ask if the class has any bugs;
- Etc.

But pay attention. Because that will send the entire code from opened file to API, this can increase the tokens consume. And also will can reach the token limit per request sooner, but you can increase this limit using the GPT-3.5-Turbo-16k model.

By executing this command, you can also hold down the SHIFT key when press the "Send Code" button so that the code will be write directly in the chat window instead of the code editor, in case you want to preserve the original code and/or analyze the response before applying it to the opened code editor.

You will find this window in menu View -> Other Windows -> Visual chatGPT Studio Turbo.

Watch here some examples using the Turbo Chat:

[<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/275615943-a37e30c3-d597-42de-8a38-7d0cdbfe942f.png" width="70%">](https://youtu.be/2NHWWXFMpd0)

## Authentication

To use this tool it is necessary to connect through the OpenAI API or through Azure OpenAI.

### By OpenAI

You need create and set an OpenAI API Key.

You can do this here: [https://beta.openai.com/account/api-keys](https://beta.openai.com/account/api-keys)

### By Azure

See for more details: [https://learn.microsoft.com/azure/cognitive-services/openai/overview](https://learn.microsoft.com/azure/cognitive-services/openai/overview).

Please read [this](https://github.com/jeffdapaz/VisualChatGPTStudio/issues/31) for some more information.

## Known Issues

Unfortunately, the API that OpenAI makes available for interacting with chatGPT has a limitation on the size of the question plus the given answer.

If the question sent is too long (for example, a method with many lines) and/or the generated response is too long, the API may cut the response or even not respond at all.

For these cases I advise you to make requests via the tool windows to customize the question in a way that chatGPT does not refuse to answer, or try modify the model options to improve the responses.

## Disclaimer

- As this extension depends on the API provided by OpenAI, there may be some change by them that affects the operation of this extension without prior notice.

- As this extension depends on the API provided by OpenAI, there may be generated responses that not be what the expected.

- The speed and availability of responses directly depend on the API provided by OpenAI.

- If you are using OpenAI service instead Azure and receive a message like "429 - You exceeded your current quota, please check your plan and billing details.", check OpenAI Usage page and see if you still have quota, example:

<img src="https://user-images.githubusercontent.com/63928228/242688025-47ec893e-401f-4edb-92a0-127a47a952fe.png" width="60%">

You can check your quota here: [https://platform.openai.com/account/usage](https://platform.openai.com/account/usage)

- If you find any bugs or unexpected behavior, please leave a comment so I can provide a fix.

## Release Notes

### 2.2.2

- Added the possibility to hold the SHIFT key when executing the "Send Code" command to write the code in the Turbo Chat Window instead of the code editor.
- Turbo Chat Code Command on options changed from "Apply the change requested by the user to the code" to "Apply the change requested by the user to the code, but rewrite the original code that was not changed" for better behavior.

### 2.2.1

- Added timeout to requests.

### 2.2.0

- Added the "Send Code" command on Turbo Chat window.

### 2.1.0

- Added the "Cancel" commands to stop receiving/waiting the requests.

### 2.0.0

- Completion models removed. Now all requests will be made through Chat Models.
- Adjusted the parameters of the Options for Azure due to no longer needing two resources.
- Some minor refactors and fixes.

### 1.13.0

- Added the new Option "Minify Requests". If true, all requests to OpenAI will be minified. Ideal to save Tokens.
- Added the new Option "Characters To Remove From Requests". Add characters or words to be removed from all requests made to OpenAI. They must be separated by commas, e.g. "a,1,TODO:,{".
- Now the progress status will show on the Tool Windows itself.
- Fix for "Add Summary For Entire Class" command when the class has "region" tags.
- Fix the "Add Comments" command to detect properly if was selected one or more lines code.

### 1.12.1

- Minor fixes to the "Add Summary For Entire Class" command.
- When displaying the Diff View for the command "Optimize (Diff View)", the file extension on Diff View will be according to the original file extension.

### 1.12.0

- Added the new command "Optimize (Diff View)".
- Minor fix to the "Add Summary For Entire Class" command where in some cases where chatGPT did not respond well, the class code was erased.
- Added Chinese readme. Thanks to [ATFmxyl](https://github.com/ATFmxyl) for the collaboration!

### 1.11.2

- Update to add summaries for constructors members when the command "Add Summary For Entire Class" is executed.

### 1.11.1

- Update to add summaries for more class members when the command "Add Summary For Entire Class" is executed.

### 1.11.0

- Added the new command "Add Summary For Entire Class".
- Added the gpt-3.5-turbo-16k model for the Turbo Chat Window.
- Added the "Base API" option.

### 1.10.0

- Added the new command "Translate".
- Grouped options for better usability.
- Added feedback when code was copied on Tool Window.

### 1.9.3

- Added option that permit to define the specific Azure Deployment ID for the Turbo Chat window.
- Added option that permit to define the Azure API version for the Turbo Chat window.

### 1.9.2

- Removed the "one method selection" limit.

### 1.9.1

- Changed extension icon for VS 2019 edition.

### 1.9.0

- Added Visual Studio 2019 compatibility. Thanks to [przemsiedl](https://github.com/przemsiedl).
- Fixed proxy connection (experimental). Thanks to [52017126](https://github.com/52017126).
- Added possibility to connect through Azure (experimental). Thanks to [Rabosa616](https://github.com/Rabosa616).
- The CodeDavinci and CodeCushman models have been removed as they have been deprecated by OpenAI. Thanks to [ekomsctr](https://github.com/ekomsctr) for the feedback about this.
- Fixed comment chars for the commands "Explain" and "Find Bugs" when the language is not C#.
- Added option to define the OpenAI Organization.
- Added the GTP-4 model language for the TurboChat Window (experimental).
- Added feedback when code was copied on TurboChat Window.

### 1.8.0

- Added proxy connection (experimental). Thanks to [SundayCoding](https://github.com/SundayCoding).

### 1.7.5

- Improvements on "Add Summary" command.

### 1.7.4

- Some improvements on turbo chat window.
- Added the new option parameter "Single Response". If true, the entire response will be displayed at once on code editor. Less undo history but longer waiting time.

### 1.7.3

- Improvements on turbo chat window. Added syntax highlight, vertical scroll and copy button.
- Improved TSQL syntax detection.
- Now when a context menu command is executed holding the SHIFT key, the request made is also written in the request box.

### 1.7.2

- Fixed a bug when the selected code has some special characters like '<' and '>'.

### 1.7.1

- Added icons to the context menu items.
- Improved Api Token validation. Now it's not necessary restart the Visual Studio after set the token at first time, and avoided a bug related.

### 1.7.0

- Added the new "Visual chatGPT Studio Turbo" tool window.
- Sometimes when performing the "Add Summary" command, the API ends up adding the characters "{" and/or "}" to its response. Therefore I am removing these characters from the response when they are returned as a result of this command.
- Added the new custom commands Before, After and Replace.
- Added the "Stop Sequences" on options (Credits to [graham83](https://github.com/graham83)).

### 1.6.1

- Fixed a bug introduced on previous release for the "Explain, "Add Comments" and "Add Summary" commands.

### 1.6.0

- Removed mention to Visual Studio 2019 due incompatibility.
- Added comment prefix for "Explain" and "Find Bugs" commands.
- Added line break after 160 characters on the same line for "Explain" and "Find Bugs" commands.
- Do nothing when API send only break lines on response begin (avoid new brank lines).
- Commands moved to a submenu.
- Added shortcuts to the commands.
- Fixed error when executing commands with SHIFT key in some scenarios.

### 1.5.1

- Redirect the commands responses to the tool window. To do, press and hold the SHIFT key and select the command. The response will be written on tool window instead on the code editor.

### 1.5.0

- Added syntax highlight to text editor on tool window
- Now text editor show the lines number.

### 1.4.0

- Added the possibility to customize the commands through the options.
- Added the possibility to resize the text editor on tool window.
- Added a shortcut to "Request" button on tool window. Now you can send the request just pressing CTRL+Enter.

### 1.3.2

- Now the extension will show the OpenAI API error detail when it occurs. In this way you can know what is really happening:

![image](https://user-images.githubusercontent.com/63928228/223844744-e7a9b350-b590-40c6-a36a-6ce8d327eb9f.png)

### 1.3.1

- Added support for Visual Studio ARM architecture.

### 1.3.0

- Improved the "Add Comments" command.

- Now it's possible to customize the OpenAI API requests parameters:

<img src="https://user-images.githubusercontent.com/63928228/223844877-d11a524b-472d-4046-94c5-70e6d3a49752.png" width="70%">

But only change the default values if you know what you're doing, otherwise you might experience some unexpected behavior.

For more details about each property, see the OpenAI documentation: [https://platform.openai.com/docs/api-reference/completions/create](https://platform.openai.com/docs/api-reference/completions/create).

You can play with these parameters [here](https://platform.openai.com/playground) before changing them in the plugin.

### 1.2.3

- Fixed commands when select code from bottom to top or end to start (Thanks to Tim Yen to alert me about this issue).
- More improvements on "Summary" command.
- Minor bugs fixes.

### 1.2.2

- Improved the "Summary" command.

### 1.2.1

- Improved the "Summary" command. Now most of times the chatGPT will not write again the method/property head.
- Added a valiation to avoid the chatGPT response be write on wrong line.
- Other minor fixes.

### 1.2.0

- Added the new command "Complete". Start write a method, select it and ask for complete.
- Now the extension is compatible with Visual Studio 2019.
- Minor bugs fixes.

### 1.1.0

- I figure out a way to improve the size of the requests. This will resolve the most of "Some error occur. Please try again or change the selection." error. Now in most cases instead of failing, if the data limit is exceeded chatGPT will not write the entire response.

### 1.0.1 

- Fixed a bug that prevented the window tool from working after closing and reopening Visual Studio .
- Added a feedback when the extension is waiting for chatGPT response:

![image](https://user-images.githubusercontent.com/63928228/223844933-2e6775d6-c657-42a1-9417-111aa3fb3245.png)
