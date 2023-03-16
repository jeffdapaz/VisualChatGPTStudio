# Visual chatGPT Studio

This is an extension that adds chatGPT functionality directly within Visual Studio.

You will be able to consult the chatGPT directly through the text editor or through a new specific tool window.

[Watch here how this works.](https://www.youtube.com/watch?v=h_wUl_IjWRU)

## Features in text editor

Select a method and right click on text editor and you see these new chatGPT commands:

![image](https://user-images.githubusercontent.com/63928228/223844534-42369d55-db63-43f7-9cc3-7ec3ac08c67e.png)

- **Complete:** Start write a method, select it and ask for complete.
- **Add Tests:** Create unit tests for the selected method.
- **Find Bugs:** Find bugs for the selected method.
- **Optimize:** Optimize the selected method.
- **Explain:** Write an explanation of the selected method.
- **Add Comments:** Add comments for the selected method.
- **Add Summary:** Add Summary for C# methods.
- **Ask Anything:** Write a question on the code editor and wait for an answer.

And if you desire that the responses be written on tool window instead on the code editor, press and hold the SHIFT key and select the command.

If you want chatGPT to respond in another language and/or want to customize the commands for some reason, you can edit the default commands through the options:

![image](https://user-images.githubusercontent.com/63928228/225159610-8a1c4dd0-a42d-4c24-b7c2-588f78cef043.png)

For example, by changing the "Explain" prompt of the Explain command to "Explicar en español" (or just "Explicar"), the OpenAI API will write the comments in Spanish instead of using the default English command.

## Features by tool window

In this new window editor you can interact directly with chatGPT as if you were in the chatGPT portal itself:

![image](https://user-images.githubusercontent.com/63928228/225486306-d29b1ec3-2ccd-4d74-8153-806a84abe5ea.png)

You will find this window in menu View -> Other Windows -> Visual chatGPT Studio.

## Authentication

To use this tool you need create and set an OpenAI API Key.

You can do this here: [https://beta.openai.com/account/api-keys](https://beta.openai.com/account/api-keys)

## Known Issues

Unfortunately, the API that OpenAI makes available for interacting with chatGPT has a limitation on the size of the question plus the given answer.

If the question sent is too long (for example, a method with many lines) and/or the generated response is too long, the API may cut the response or even not respond at all.

For these cases, I advise you to make requests via the tool window to customize the question in a way that chatGPT does not refuse to answer.

In the future if I find a way around this limitation, I will post an update.

## Disclaimer

- As this extension depends on the API provided by OpenAI, there may be some change by them that affects the operation of this extension without prior notice.

- As this extension depends on the API provided by OpenAI, there may be generated responses that not be what the expected.

- The speed and availability of responses directly depend on the API provided by OpenAI.

## Release Notes

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

![image](https://user-images.githubusercontent.com/63928228/223844877-d11a524b-472d-4046-94c5-70e6d3a49752.png)

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
