# Visual chatGPT Studio <img src="https://user-images.githubusercontent.com/63928228/278760982-5a3be81c-0cb0-4e59-98f6-705b371553e5.png" width="3.5%"> 

🌎 English | [Chinese](https://github.com/jeffdapaz/VisualChatGPTStudio/blob/master/README-zh.md)

👉 For Visual Studio 2022: [here](https://marketplace.visualstudio.com/items?itemName=jefferson-pires.VisualChatGPTStudio)

👉 For Visual Studio 2019: [here](https://marketplace.visualstudio.com/items?itemName=jefferson-pires.VisualChatGPTStudio2019)

## Description 💬

This is an extension that adds chatGPT functionality directly within Visual Studio.

You will be able to consult the chatGPT directly through the text editor or through a new specifics tool windows.

Watch here some examples:

[<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/275614252-5f824ba6-df13-45e3-928e-086609fe1bcd.png" width="70%">](https://youtu.be/eU5hEh6e5Ow)

## Features on code editor 👩‍💻

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

## Edit the Commands 📐

The pre-defined commands can be edited at will to meet your needs and the needs of the project you are currently working on.

It is also possible to define specific commands per Solution or Project. If you are not working on a project that has specific commands for it, the default commands will be used.

Some examples that you can do:

- **Define specific framework or language:** For example, you can have specific commands to create unit tests using MSTests for a project and XUnit for another one.
- **Work in another language:** For example, if you work in projects that use another language different that you mother language, you can set commands for your language and commands to another language for another projects.
- **ETC**.

<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/281889918-f6bae902-077b-4688-84ca-d661a9336866.png" width="100%">

## Features by `Visual chatGPT Studio` tool window 🛠

In this tool window you can ask questions to chatGPT and receive answers directly in it.

![https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/2.6.0/1709307485035/image__6.png](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/2.6.0/1709307485035/image__6.png)

This window can also be used to redirect the responses of commands executed in the code editor to it, holding the SHIFT key while executing a command, this way you can avoid editing the code when you don't want to, or when you want to validate the result before inserting it in the project.

## Automatically create comments for your changes 📑

In this window it will also be possible to create a git push comments based on pending changes by clicking on this button:

![https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/2.6.0/1709307485035/image__9.png](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/2.6.0/1709307485035/image__9.png)

No more wasting time thinking about what you are going to write for your changes!

Through the `Generate Git Changes Comment Command` option of this extension, you can edit the request command. Ideal if you want comments to be created in a language other than English, and/or if you want the comment to follow some other specific format, etc.

You will find this window in menu View -> Other Windows -> Visual chatGPT Studio.

## Features by `Visual chatGPT Studio Turbo` tool window 🚀

In this window editor you can interact directly with chatGPT as if you were in the chatGPT portal itself:

Unlike the previous window, in this one the AI "remembers" the entire conversation:

![image](https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/ff47bf5c-8324-46ba-a039-173c172337e0)

You can also interact with the opened code editor through the `Send Code` button. Using this button the OpenAI API becomes aware of all the code in the opened editor, and you can request interactions directly to your code, for example:

- Ask to add new method on specific line, or between two existing methods;
- Change a existing method to add a new parameter;
- Ask if the class has any bugs;
- Etc.

But pay attention. Because that will send the entire code from opened file to API, this can increase the tokens consume. And also will can reach the token limit per request sooner depending the model you are using. Set a model with large tokens limit can solve this limitation.

By executing this command, you can also hold down the SHIFT key when press the `Send Code` button so that the code will be write directly in the chat window instead of the code editor, in case you want to preserve the original code and/or analyze the response before applying it to the opened code editor.

You will also be able to keep multiple chats open at the same time in different tabs. And each chat is kept in history, allowing you to continue the conversation even if Visual Studio is closed:

<img src="https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/2.4.0/1700946186856/image__8.png" width="60%">

You will find this window in menu View -> Other Windows -> Visual chatGPT Studio Turbo.

Watch here some examples using the Turbo Chat:

[<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/275615943-a37e30c3-d597-42de-8a38-7d0cdbfe942f.png" width="70%">](https://youtu.be/2NHWWXFMpd0)

## Features by `Visual chatGPT Studio Solution Context` tool window 📌

<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/282463640-4e7c24c6-41d5-4cee-aa36-f363daba6f95.png" width="75%">

Here you can add project items to the context of requests to OpenAI. Ideal for making requests that require knowledge of other points of the project.

For example, you can request the creation of a method in the current document that consumes another method from another class, which was selected through this window.

You can also ask to create unit tests in the open document of a method from another class referenced through the context.

You can also request an analysis that involves a larger context involving several classes. The possibilities are many.

But pay attention. Depending on the amount of code you add to the context, this can increase the tokens consume. And also will can reach the token limit per request sooner depending the model you are using. Set a model with large tokens limit can solve this limitation.

You will find this window in menu View -> Other Windows -> Visual chatGPT Studio Solution Context.

## Features by `Visual chatGPT Studio Code Review` tool window 🔍

The Code Review Tool Window feature is designed to enhance the development workflow by automatically generating code reviews based on Git Changes in a project. This innovative functionality aims to identify potential gaps and areas for improvement before a pull request is initiated, ensuring higher code quality and facilitating a smoother review process.

<img src="https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/2.7.0/1711660202615/image__5.png" width="75%">

### How It Works

1. **Git Changes Detection:** The feature automatically detects any changes made to the codebase in Git.

2. **Automatic Review Generation:** Upon detecting changes, the system instantly analyzes the modifications using the power of AI. It evaluates the code for potential issues such as syntax errors, code smells, security vulnerabilities, and performance bottlenecks.

3. **Feedback Provision:** The results of the analysis are then compiled into a comprehensive code review report. This report includes detailed feedback on each identified issue, suggestions for improvements, and best practice recommendations. 

4. **Integration with Development Tools:** The feature seamlessly integrates with the Visual Studio, ensuring that the code review process is a natural part of the development workflow. 

5. **Edit the command:** Through the extension options, it is possible to edit the command that requests the Code Review for customization purposes.

### Benefits

- **Early Detection of Issues:** By identifying potential problems early in the development cycle, developers can address issues before they escalate, saving time and effort.
- **Improved Code Quality:** The automatic reviews encourage adherence to coding standards and best practices, leading to cleaner, more maintainable code.
- **Streamlined Review Process:** The feature complements the manual code review process, making it more efficient and focused by allowing reviewers to concentrate on more complex and critical aspects of the code.
- **Enhanced Collaboration:** It fosters a culture of continuous improvement and learning among the development team, as the automated feedback provides valuable insights and learning opportunities.

## Authentication 🔑

To use this tool it is necessary to connect through the OpenAI API, Azure OpenAI, or any other API that is OpenAI API compatible.

### By OpenAI

1 - Create an account on OpenAI: https://platform.openai.com

2 - Generate a new key: https://platform.openai.com/api-keys

3 - Copy and past the key on options and set the `OpenAI Service` parameter as `OpenAI`: 

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/09a93cc9-c35d-4fee-b3a1-05f2dd0212f1" width="75%">

### By Azure

1 - First, you need have access to Azure OpenAI Service. You can see more details [here](https://learn.microsoft.com/en-us/legal/cognitive-services/openai/limited-access?context=%2Fazure%2Fcognitive-services%2Fopenai%2Fcontext%2Fcontext).

2 - Create an Azure OpenAI resource, and set the resource name on options. Example:

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/8bf9111b-cc4d-46ac-a4f2-094e83922d95" width="60%">

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/1e9495a7-d626-4845-af7f-ae6f84139d87" width="75%">

3 - Copy and past the key on options and set the `OpenAI Service` parameter as `AzureOpenAI`: 

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/2f881df1-a95f-4016-bf39-9cf2e83aef0e" width="75%">

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/8b035735-ce8e-4f25-a42e-6a0d14058c98" width="75%">

4 - Create a new deployment through Azure OpenAI Studio, and set the name:

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/3914ddf3-e0c5-4edd-9add-dab5aba12aa9" width="40%">

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/195539ac-8d0b-4284-bac4-de345464ed08" width="75%">

5 - Set the Azure OpenAI API version. You can check the available versions [here](https://learn.microsoft.com/en-us/azure/ai-services/openai/reference#completions).

### By Others Customs LLM

Is possible to use a service that is not the OpenAI or Azure API, as long as this service is OpenAI API compatible.

This way, you can use APIs that run locally, such as Meta's llama, or any other private deployment (locally or not).

To do this, simply insert the address of these deployments in the `Base API URL` parameter of the extension.

It's worth mentioning that I haven't tested this possibility for myself, so it's a matter of trial and error, but I've already received feedback from people who have successfully doing this.

## Known Issues ⚠

Unfortunately, the API that OpenAI makes available for interacting with chatGPT has a limitation on the size of the question plus the given answer.

If the question sent is too long (for example, a method with many lines) and/or the generated response is too long, the API may cut the response or even not respond at all.

For these cases I advise you to make requests via the tool windows to customize the question in a way that chatGPT does not refuse to answer, or try modify the model options to improve the responses.

## Disclaimer 👋

- As this extension depends on the API provided by OpenAI, there may be some change by them that affects the operation of this extension without prior notice.

- As this extension depends on the API provided by OpenAI, there may be generated responses that not be what the expected.

- The speed and availability of responses directly depend on the API provided by OpenAI.

- If you are using OpenAI service instead Azure and receive a message like `429 - You exceeded your current quota, please check your plan and billing details.`, check OpenAI Usage page and see if you still have quota, example:

<img src="https://user-images.githubusercontent.com/63928228/242688025-47ec893e-401f-4edb-92a0-127a47a952fe.png" width="60%">

You can check your quota here: [https://platform.openai.com/account/usage](https://platform.openai.com/account/usage)

- If you find any bugs or unexpected behavior, please leave a comment so I can provide a fix.

## Donations 🙏

☕ Buy me a coffee and support me to empower you more. Thank you! 

[<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/278758680-f5fc9df2-a330-4d6a-ae13-9190b7b8f57b.png" width="20%">](https://www.paypal.com/donate/?hosted_button_id=2Y55G8YYC6Q3E)

## Dependencies ⚙

- [AvalonEdit](https://github.com/icsharpcode/AvalonEdit)
- [LibGit2Sharp](https://github.com/libgit2/libgit2sharp)
- [OpenAI-API-dotnet](https://github.com/OkGoDoIt/OpenAI-API-dotnet)
- [sqlite-net-pcl](https://github.com/praeclarum/sqlite-net)
- [MdXaml](https://github.com/whistyun/MdXaml)
- [VsixLogger](https://github.com/madskristensen/VsixLogger)
- [Community.VisualStudio.Toolkit.17](https://github.com/VsixCommunity/Community.VisualStudio.Toolkit)

## Release Notes 📜

### 2.8.1

- Updated `Ask Anything` command to send `Tool Window System Message` as System Message.

### 2.8.0

- Added the new option `Tool Window System Message` in the Extension's Options to possibility customizing the System Message for the `Tool Window` chat requests.
- Fixed a bug that made all requests be made twice, that causing increase wait time from responses and consume unnecessary tokens.
- Fixed a bug related to the `Base API URL` option, which could not take effect if changed.
- Improved the `Add Summary For Entire Class` command to avoid bad formatting.

### 2.7.2

- Added the new Model option GPT-4o.
- Now the GPT-4-Turbo model points to the default version (the latest available). Before, it was using the preview version.

### 2.7.1

- Added the new `Custom Model` option.

### 2.7.0

- Added the new `Code Review` functionality.

### 2.6.0

- Added the new `Generate Git Changes Comment` Command on the Visual chatGPT Studio Tool Window.
- Improvements for the Add Summary, Add Tests, Complete and Optimize commands result. In most cases will only write the code from OpenAI responses, ignoring any additional comments that could come with the OpenAI response. To make this possible, unfortunately I had to disable the ability to write responses via stream for these commands, so now they will work as "Single Response" regardless of the option selected via Options.
- Other minor improvements.

### 2.5.2

- Added logging writing in case of exceptions in the Visual Studio Output window.
- Small adjustment to the spacing of chat items.
- A fix was made to the requests made to the APIs, where the content was not in accordance with the OpenAI standard. This could cause errors.

### 2.5.1

- Change so that the GPT_4_Turbo model always automatically points to the latest version made available by OpenAI. Currently, the latest version is gpt-4-0125-preview (see more details [here](https://openai.com/blog/new-embedding-models-and-api-updates)).
- Update the Cancel command icon.

### 2.5.0

- Update appearance and user interface.
- Small improvement in the automatic creation of titles for chats.

### 2.4.5

- Fixed the compatibility with the Visual Studio ARM edition.
- Small improvement in the automatic creation of titles for chats.

### 2.4.4

- Small improvements in adding comment characters to some commands that write comments in the code editor.
- Small improvement in the automatic creation of titles for chats.

### 2.4.3

- Added the gpt-4-32k model.
- Removed the gpt-3.5-turbo-16k model. OpenAI currently points to gpt-3.5-turbo model.
- Fixed a bug that was causing duplicate requests to the API.
- Improved verification of changes to connection-related options to apply them in execution mode, avoiding the need to restart Visual Studio to take effect.

### 2.4.2

- Added the Word Wrap switch button on Tool Window.
- Added the feature to automatically remove early chat messages from conversation history when the context limit has reached.

### 2.4.1

- Fixed a bug introduced in the last release when trying to use the Turbo Chat Window. The bug is related to the creation of the file to store the chat history.

### 2.4.0

- Added tabs to the Turbo Chat.
- Added Chats history.
- Fixed a behavior that cause the opening of all the extension Tool Windows when opened one of them.

### 2.3.0

- Added the new models gpt-3.5-turbo-1106 and gpt-4-1106-preview (maybe not work with Azure yet). See [here](https://openai.com/blog/new-models-and-developer-products-announced-at-devday) for more details about these new models.
- Added the new options "Log Request" and "Log Responses". If ON, all requests and/or responses to OpenAI will be logged to the Output window.
- Commands Options reformulated. Now it's possible set commands by Projects and Solutions.
- Added the new "Visual chatGPT Studio Solution Context" window, where it is possible to add items from projects to the context of requests to OpenAI.
- Fixed a bug that was preventing an error message from being displayed in some cases if communication with OpenAI failed. As a result, the execution of the request was not interrupted.

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
- Added a validation to avoid the chatGPT response be write on wrong line.
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