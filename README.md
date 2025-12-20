# Visual chatGPT Studio <img src="https://user-images.githubusercontent.com/63928228/278760982-5a3be81c-0cb0-4e59-98f6-705b371553e5.png" width="3.5%"> 

🌎 English | [Chinese](https://github.com/jeffdapaz/VisualChatGPTStudio/blob/master/README-zh.md)

👉 For Visual Studio 2022: [here](https://marketplace.visualstudio.com/items?itemName=jefferson-pires.VisualChatGPTStudio)

👉 For Visual Studio 2019: [here](https://marketplace.visualstudio.com/items?itemName=jefferson-pires.VisualChatGPTStudio2019)

## Description 💬

Visual chatGPT Studio is a powerful extension that integrates advanced AI capabilities directly into Visual Studio, enhancing your coding experience. This extension provides a suite of tools that leverage AI to assist you in various coding tasks. 

With Visual chatGPT Studio, you can receive intelligent code suggestions, generate unit tests, find bugs, optimize code, and much more—all from within your development environment. The extension allows you to interact with AI in a way that streamlines your workflow, making coding more efficient and enjoyable.

Watch here some examples:

[<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/275614252-5f824ba6-df13-45e3-928e-086609fe1bcd.png" width="70%">](https://youtu.be/eU5hEh6e5Ow)

## Table of Contents 📚

- [`Copilot` Functionality](#1)
- [Features on code editor](#2)
- [Edit the Commands](#3)
- [Features by `Visual chatGPT Studio` tool window](#4)
- [Automatically create comments for your GIT changes](#5)
- [Features by `Visual chatGPT Studio Turbo` chat tool window](#6)
- [SQL Server Agent](#7)
- [API Service Agent](#7-1)
- [Commands Shortcuts and File/Method References](#8)
- [Features by `Visual chatGPT Studio Solution Context` tool window](#9)
- [Features by `Visual chatGPT Studio Code Review` tool window](#10)
- [Computer Use Automation (Beta)](#20)
- [Also Check Out](#11)
- [Authentication](#12)
	- [By OpenAI](#12-1)
	- [By Azure (API Key Authentication)](#12-2)
	- [By Azure (Entra ID Authentication)](#12-3)
	- [By Other Customs LLMs](#12-4)
- [Use Completion API for Commands](#13)
- [Known Issues](#14)
- [Disclaimer](#15)
- [Donations](#16)
- [Dependencies](#17)
- [Release Notes](#18)

<a id="1"></a>
## Copilot Functionality (only for Visual Studio 2022) 🤖

The Copilot functionality enhances your coding experience by providing intelligent code suggestions as you type. 

When you start writing code, simply press the Enter key to receive contextual suggestions that can help you complete your code more efficiently. Confirm the suggestion pressing the TAB key.

![image](https://github.com/user-attachments/assets/fc898671-b480-4c6d-9115-8c6abc5f5820)

You can disable the Copilot Functionality through the options if you desire.

<a id="2"></a>
## Features on code editor 👩‍💻

Select a method and right click on text editor and you see these new commands:

![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/2.1.0/1697062741546/image__2.png)

- **Complete:** Start write a method, select it and ask for complete.
- **Add Tests:** Create unit tests for the selected method.
- **Find Bugs:** Find bugs for the selected code.
- **Optimize:** Optimize the selected code.
- **Optimize (Diff View):** Optimize the selected code, however, instead of the result being written in the code editor, a new window will open where you can compare the original code with the version optimized by AI.
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

<a id="3"></a>
## Edit the Commands 📐

The pre-defined commands can be edited at will to meet your needs and the needs of the project you are currently working on.

It is also possible to define specific commands per Solution or Project. If you are not working on a project that has specific commands for it, the default commands will be used.

Some examples that you can do:

- **Define specific framework or language:** For example, you can have specific commands to create unit tests using MSTests for a project and XUnit for another one.
- **Work in another language:** For example, if you work in projects that use another language different that you mother language, you can set commands for your language and commands to another language for another projects.
- **ETC**.

<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/281889918-f6bae902-077b-4688-84ca-d661a9336866.png" width="100%">

<a id="4"></a>
## Features by `Visual chatGPT Studio` tool window 🛠

In this tool window you can ask questions to the AI and receive answers directly in it.

![https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/2.6.0/1709307485035/image__6.png](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/2.6.0/1709307485035/image__6.png)

This window can also be used to redirect the responses of commands executed in the code editor to it, holding the SHIFT key while executing a command, this way you can avoid editing the code when you don't want to, or when you want to validate the result before inserting it in the project.

You will also be able to attach images and make requests such as asking for code that implements the same layout as the attached image (not all models accept images).

<a id="5"></a>
## Automatically create comments for your GIT changes 📑

In this window it will also be possible to create a git push comments based on pending changes by clicking on this button:

![https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/2.6.0/1709307485035/image__9.png](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/2.6.0/1709307485035/image__9.png)

No more wasting time thinking about what you are going to write for your changes!

Through the `Generate Git Changes Comment Command` option of this extension, you can edit the request command. Ideal if you want comments to be created in a language other than English, and/or if you want the comment to follow some other specific format, etc.

You will find this window in menu View -> Other Windows -> Visual chatGPT Studio.

<a id="6"></a>
## Features by `Visual chatGPT Studio Turbo` chat tool window 🚀

In this window editor you can interact directly with the AI as if you were in the chatGPT portal itself:

Unlike the previous window, in this one the AI "remembers" the entire conversation:

![image](https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/ff47bf5c-8324-46ba-a039-173c172337e0)

You can also interact with the opened code editor through the `Send Code` button. Using this button the OpenAI API becomes aware of all the code in the opened editor, and you can request interactions directly to your code, for example:

- Ask to add new method on specific line, or between two existing methods;
- Change a existing method to add a new parameter;
- Ask if the class has any bugs;
- Etc.

You will also be able to keep multiple chats open at the same time in different tabs. And each chat is kept in history, allowing you to continue the conversation even if Visual Studio is closed:

<img src="https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/2.4.0/1700946186856/image__8.png" width="60%">

As in the 'Tool Window', it is also possible to attach images here and make requests regarding them (not all models accept images).

You will find this window in menu View -> Other Windows -> Visual chatGPT Studio Turbo.

Watch here some examples using the Turbo Chat:

[<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/275615943-a37e30c3-d597-42de-8a38-7d0cdbfe942f.png" width="70%">](https://youtu.be/2NHWWXFMpd0)

<a id="7"></a>
## SQL Server Agent 🕵️

With the SQL Server Agent integrated into the Turbo Chat window, you can ask the AI to execute SQL scripts using natural language.

Follow these steps to make the most of this feature:

1. **Add SQL Server database connections**  
   In the **Server Explorer** window of Visual Studio, configure connections to the desired databases. Each connection must point to a specific database:

   ![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/4.0.0/1740616321738/1.png)

2. **Access the database icon in Turbo Chat**  
   In the Turbo Chat window, click on the database icon:

   ![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/4.0.0/1740616321738/2.png)

3. **Select the database from the combobox**  
   Use the combobox to choose which database you want the AI to work with. You can add as many databases as needed:

   ![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/4.0.0/1740616321738/3.png)

4. **Request actions in the chat**  
   After adding the database(s) to the chat context, request any SQL action, whether DML (Data Manipulation Language) or DDL (Data Definition Language):

   ![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/4.0.0/1740616321738/4.png)

5. **View query results**  
   If you request a query, the result will be displayed based on the last execution performed:

   ![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/4.0.0/1740616321738/5.png)

6. **Important considerations**  
   - The **connection string** is never sent to the AI. Only the server name, database name, and schema are shared.  
   - Query data is never sent to the AI, both for data protection reasons and to save token usage. The only exception is when the AI executes a **SCALAR** script (returning only one record and one column). 

<a id="7-1"></a>
## API Service Agent 🧙‍♂️

An AI-powered agent capable of interacting with most REST and SOAP services. It dynamically processes API structures, enabling seamless communication with various web services while keeping authentication details securely managed by the user.

Follow these steps to make the most of this feature:

1. **Add API definitions**  
   In the extension options window, add definitions to the desired APIs:

   ![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio/5.0.0/1741729906077/image__12.png)

   - **Identification**: Enter a unique name to identify the API.
   - **Base URL**: Enter the base URL of the API.
   - **Key/Values**: Define key-value pairs to be included in API requests, or to replace the key/values defined by the AI. Ideal for inserting authentication/authorization key/values, or to ensure that all calls have a certain key/value.
   - **Send Responses to AI**: If checked, all API responses will be forwarded to the AI so it can process and retain them in its context. Otherwise, the AI will only receive the HTTP status, and the responses will be displayed directly in the chat. This option is ideal if you want to protect data and save tokens.
   - **Definition**: Enter the API's definition (e.g., OpenAPI, Swagger, SOAP) here. This allows the AI to understand the API's structure and capabilities for making requests.

2. **Access the 'API' icon in Turbo Chat**  
   In the Turbo Chat window, click on the API icon:

   ![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio/5.0.0/1741729906077/image__13.png)

3. **Select the API definition from the combobox**  
   Use the combobox to choose which APIs you want the AI to work with. You can add as many APIs as needed:

   ![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio/5.0.0/1741729906077/image__15.png)

4. **Request actions in the chat**  
   After adding the API(s) to the chat context, make requests for AI to interact with the APIs:

   ![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio/5.0.0/1741729906077/image__16.png)

   Or if you prefer that the AI only receives the status code from the APIs, without the actual responses (after disabled the "Send Responses to AI" parameter):

   ![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio/5.0.0/1741729906077/image__17.png)

5. **Important considerations**  
   - The AI ​​never has knowledge of the keys/values ​​configured through the options, so they are ideal for authentication tokens.
   - The AI ​​never has knowledge of the Base URL.
   - However, if for some reason you want the AI ​​to have knowledge of the above data, you will have to inform the AI ​​via requests.
   - Depending on the type of authentication used in the API, you will have to manually authenticate, and then parameterize the token via options, or inform the authentication data via request.
   - In my tests, I noticed that the AI may have difficulties dealing with complex APIs and/or complex endpoints. For these cases, I suggest trying to understand the API's difficulties through the logs in the Output Window and attempting to guide it through requests. In general, the API agent may not be suitable for complex cases.

<a id="8"></a>
## Commands Shortcuts and File/Method References ⌨️

You can reference the pre-defined commands, files and methods directly in the request fields. This means you can easily insert the pre-definied commands, as well file paths or method names into your requests, making it more efficient to work with your code.

To reference predefined commands, access them with the key "/". To reference files or methods, access them with the key "#".

Watch the demonstration of this feature in action:

![complete](https://github.com/user-attachments/assets/f95ad9bd-ce5a-4299-9593-9e098e47e415)

<a id="9"></a>
## Features by `Visual chatGPT Studio Solution Context` tool window 📌

<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/282463640-4e7c24c6-41d5-4cee-aa36-f363daba6f95.png" width="75%">

Here you can add project items to the context of requests to OpenAI. Ideal for making requests that require knowledge of other points of the project.

For example, you can request the creation of a method in the current document that consumes another method from another class, which was selected through this window.

You can also ask to create unit tests in the open document of a method from another class referenced through the context.

You can also request an analysis that involves a larger context involving several classes. The possibilities are many.

But pay attention. Depending on the amount of code you add to the context, this can increase the tokens consume. And also will can reach the token limit per request sooner depending the model you are using. Set a model with large tokens limit can solve this limitation.

You will find this window in menu View -> Other Windows -> Visual chatGPT Studio Solution Context.

<a id="10"></a>
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

<a id="20"></a>
## Computer Use Automation (Beta) 💻

Visual chatGPT Studio now includes an experimental Computer Use feature that allows the AI to perform automated actions inside Visual Studio, such as clicking, typing, scrolling, and more — simulating a user interacting with the IDE.

⚠️ Important: This feature is currently in Beta. The AI's ability to reliably execute commands in Visual Studio is limited, and the delay between commands can be quite long. This feature is intended primarily for testing AI capabilities and for fun experimentation, rather than production use.

### How to Use

- Open the Turbo Chat tool window.
- Enter your instruction prompt describing the actions you want the AI to perform inside Visual Studio (e.g., "Open the Solution Explorer and search for 'MyClass'").
- Click the "Computer Use" button to activate the feature.
- The AI will start executing the requested actions automatically.
- While the AI is executing, a status message will be displayed: "AI is executing actions. Please wait and avoid interaction until completion."
- You can cancel the execution anytime by clicking the Cancel button.

### Limitations and Notes

- The AI currently cannot fully understand or reliably control all Visual Studio UI elements.
- The interval between commands is long, so expect delays during execution.
- It's recommended to avoid interact with Visual Studio while the AI is performing its actions, as it is based on the current position of the mouse pointer.
- Because of these limitations, this feature is best used for testing AI capabilities and experimentation.
- You need use a model compatible with computer-use feature. See OpenAI or Azure OpenAI documentation about this.
- If you are using Azure OpenAI, it's necessary have a computer-use-preview deployment withing the same resource parametrized through options.

<a id="11"></a>
## Also Check Out 🔗

### Backlog chatGPT Assistant <img src="https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/backlogchatgptassistant/1.0.0/1725050136867/Microsoft.VisualStudio.Services.Icons.Default" width="5%"> 

If you find Visual chatGPT Studio helpful, you might also be interested in my other extension, [Backlog chatGPT Assistant](https://marketplace.visualstudio.com/items?itemName=jefferson-pires.BacklogChatGPTAssistant). This powerful tool leverages AI to create and manage backlog items on Azure Devops directly within Visual Studio. Whether you're working with pre-selected work items, user instructions, or content from DOCX and PDF files, this extension simplifies and accelerates the process of generating new backlog items.

<a id="12"></a>
## Authentication 🔑

To use this tool it is necessary to connect through the OpenAI API, Azure OpenAI, or any other API that is OpenAI API compatible.

<a id="12-1"></a>
### By OpenAI

1 - Create an account on OpenAI: https://platform.openai.com

2 - Generate a new key: https://platform.openai.com/api-keys

3 - Copy and past the key on options and set the `OpenAI Service` parameter as `OpenAI`: 

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/09a93cc9-c35d-4fee-b3a1-05f2dd0212f1" width="75%">

<a id="12-2"></a>
### By Azure (API Key Authentication)

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

6 - Optional: Instead of allowing the extension to automatically build the connection URL to Azure OpenAI from the parameters 'Resource Name', 'Deployment Name', and 'API Version', manually define the connection URL through the 'Azure URL Override' parameter, which is ideal for cases where the endpoint for Azure OpenAI is custom for some reason. When a value is set for this parameter, the other parameters ('Resource Name', 'Deployment Name', 'API Version') will be ignored.

<a id="12-3"></a>
### By Azure (Entra ID Authentication)

In addition to API Key authentication, you can now authenticate to Azure OpenAI using Microsoft Entra ID. To enable this option:

1 - Ensure your Azure OpenAI deployment is registered in Entra ID, and the user has access permissions.

2 - In the extension settings, set the parameter Entra ID Authentication to true.

3 - Define the Application Id and Tenant Id for your application in the settings.

4 - The first time you run any command, you will be prompted to log in using your Microsoft account.

5 - For more details on setting up Entra ID authentication, refer to the documentation [here](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/managed-identity).

<a id="12-4"></a>
### By Others Customs LLMs

Is possible to use a service that is not the OpenAI or Azure API, as long as this service is OpenAI API compatible.

This way, you can use APIs that run locally, such as Meta's llama, or any other private deployment (locally or not).

To do this, simply insert the address of these deployments in the `Base API URL` parameter of the extension.

It's worth mentioning that I haven't tested this possibility for myself, so it's a matter of trial and error, but I've already received feedback from people who have successfully doing this.

<a id="13"></a>
## Use Completion API for Commands 🧠

This feature introduces the ability to utilize the Completion API for handling command requests within the code editor. When enabled, all requests for code completion will use the Completion model and API instead of the Chat API. This feature is particularly useful for scenarios where specific tuning of parameters for completions is better.

Note: This functionality does not support Azure OpenAI API.

How to Enable the Feature:

- Open the Options for the extension within Visual Studio.
- Navigate to the General category.
- Locate the Use Completion API for Commands setting and set it to True.
- Configuring Completion Parameters

<a id="14"></a>
## Known Issues 🐛

- **Issue 1:** Occasional delays in AI response times.
- **Issue 2:** AI can hallucinate in its responses, generating invalid content.
- **Issue 3:** If the request sent is too long and/or the generated response is too long, the API may cut the response or even not respond at all.
- **Workaround:** Retry the request changing the model parameters and/or the command.

<a id="15"></a>
## Disclaimer 👋

- Some features relies on OpenAI Function feature ([https://platform.openai.com/docs/guides/function-calling](https://platform.openai.com/docs/guides/function-calling)), so you need use a model that supports it for better experience.

- As this extension depends on the API provided by OpenAI or Azure, there may be some change by them that affects the operation of this extension without prior notice.

- As this extension depends on the API provided by OpenAI or Azure, there may be generated responses that not be what the expected.

- The speed and availability of responses directly depend on the API.

- If you are using OpenAI service instead Azure and receive a message like `429 - You exceeded your current quota, please check your plan and billing details.`, check OpenAI Usage page and see if you still have quota, example:

<img src="https://user-images.githubusercontent.com/63928228/242688025-47ec893e-401f-4edb-92a0-127a47a952fe.png" width="60%">

You can check your quota here: [https://platform.openai.com/account/usage](https://platform.openai.com/account/usage)

- If you find any bugs or unexpected behavior, try first updating Visual Studio to its latest version. If not resolved, please, leave a comment or open an issue on Github so I can provide a fix.

<a id="16"></a>
## Donations 🙏

☕️ If you find this extension useful and want to support its development, consider [buying me a coffee](https://www.paypal.com/donate/?hosted_button_id=2Y55G8YYC6Q3E). Your support is greatly appreciated!

[<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/278758680-f5fc9df2-a330-4d6a-ae13-9190b7b8f57b.png" width="20%">](https://www.paypal.com/donate/?hosted_button_id=2Y55G8YYC6Q3E)

<a id="17"></a>
## Dependencies ⚙

- [AvalonEdit](https://github.com/icsharpcode/AvalonEdit)
- [LibGit2Sharp](https://github.com/libgit2/libgit2sharp)
- [sqlite-net-pcl](https://github.com/praeclarum/sqlite-net)
- [MdXaml](https://github.com/whistyun/MdXaml)
- [VsixLogger](https://github.com/madskristensen/VsixLogger)
- [Community.VisualStudio.Toolkit.17](https://github.com/VsixCommunity/Community.VisualStudio.Toolkit)
- [Markdig](https://github.com/xoofx/markdig)
- [Markdig.SyntaxHighlighting](https://github.com/RichardSlater/Markdig.SyntaxHighlighting/)
- [InputSimulator](https://github.com/michaelnoonan/inputsimulator)

<a id="18"></a>
## Release Notes 📜

### 5.6.1

- Fix Copilot suggestions not appearing in Visual Studio 2026.
- Fix API agent not working properly in some scenarios.
- Fix "Send Code" feature in Turbo Chat not working properly in some scenarios.

### 5.6.0

- Improvements on copilot feature to suggests completions more often.
- Added option to configure a dedicated Copilot model (or Azure deployment) for inline suggestions, with selectable Default / Completion / Specific-Chat modes.
- The "Apply" button in Turbo Chat now can applies only the selected code block when a selection is present; otherwise, it applies the entire code.
- Added the "Apply" button in the "Visual chatGPT Studio" window.

### [More Change Logs](https://github.com/jeffdapaz/VisualChatGPTStudio/blob/master/ReleaseNotes.md)