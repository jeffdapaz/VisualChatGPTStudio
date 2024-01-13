# Visual chatGPT Studio <img src="https://user-images.githubusercontent.com/63928228/278760982-5a3be81c-0cb0-4e59-98f6-705b371553e5.png" width="3.5%"> 

🌎 [English](https://github.com/jeffdapaz/VisualChatGPTStudio/blob/master/README.md) | Chinese

👉 适用于 Visual Studio 2022 的插件在 [这里](https://marketplace.visualstudio.com/items?itemName=jefferson-pires.VisualChatGPTStudio)。

👉 适用于 Visual Studio 2019 的插件在 [这里](https://marketplace.visualstudio.com/items?itemName=jefferson-pires.VisualChatGPTStudio2019)。

## 描述 💬

这是一个在 Visual Studio 中直接增加了 ChatGPT 功能的扩展。

您可以通过文本编辑器直接向 ChatGPT 咨询，也可以通过新的特定工具窗口进行操作。

在这里观看一些示例：

[<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/275614252-5f824ba6-df13-45e3-928e-086609fe1bcd.png" width="70%">](https://youtu.be/eU5hEh6e5Ow)

## 代码编辑器上的功能 👩‍💻

在文本编辑器中选择一个方法，右键单击，您将看到这些新的 ChatGPT 命令：

![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/2.1.0/1697062741546/image__2.png)

- **Complete（完成）：** 开始编写一个方法，选择它并请求完成。
- **Add Tests（添加测试）：** 为所选方法创建单元测试。
- **Find Bugs（查找错误）：** 查找所选代码中的错误。
- **Optimize（优化）：** 优化所选代码。
- **Optimize（差异视图）：** 优化所选代码，但是，与结果写入代码编辑器不同，将会打开一个新窗口，您可以在其中比较 ChatGPT 优化的代码版本与原始代码的区别。
- **Explain（解释）：** 对所选代码进行解释。
- **Add Comments（添加注释）：** 为所选代码添加注释。
- **Add Summary（添加摘要）：** 为 C# 方法添加摘要。
- **Add Summary For Entire Class（为整个类添加摘要）：** 为整个 C# 类添加摘要（适用于方法、属性、枚举、接口、类等）。无需选择代码，只需运行命令即可开始流程。
- **Ask Anything（问任何问题）：** 在代码编辑器中写下问题，等待答案。
- **Translate（翻译）：** 使用翻译版本替换所选文本。在选项窗口中编辑命令，如果要将其翻译成英语以外的其他语言。
- **Custom Before（自定义前置）：** 通过选项创建一个自定义命令，将响应插入到所选代码之前。
- **Custom After（自定义后置）：** 通过选项创建一个自定义命令，将响应插入到所选代码之后。
- **Custom Replace（自定义替换）：** 通过选项创建一个自定义命令，用响应替换所选文本。
- **Cancel（取消）：** 取消接收/等待任何命令请求。

如果您希望响应写入工具窗口而不是代码编辑器，请按住 SHIFT 键并选择命令（快捷方式不起作用）。

## 编辑命令 📐

预定义的命令可以根据需要进行编辑，以满足您当前工作项目的需求。

还可以针对解决方案或项目定义特定的命令。如果您没有在具有特定命令的项目中工作，将使用默认命令。

以下是一些示例：

- **定义特定的框架或语言：** 例如，您可以为一个项目使用 MSTests 创建特定的命令，而为另一个项目使用 XUnit。
- **使用其他语言：** 例如，如果您在使用与您母语不同的另一种语言的项目中工作，您可以为您的语言设置命令，并为其他项目设置另一种语言的命令。
- **等等**。

<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/281889918-f6bae902-077b-4688-84ca-d661a9336866.png" width="100%">

## "Visual ChatGPT Studio" 工具窗口的功能 🛠

在这个工具窗口中，您可以向 ChatGPT 提问，并直接在其中收到答案。

![image](https://user-images.githubusercontent.com/63928228/225486306-d29b1ec3-2ccd-4d74-8153-806a84abe5ea.png)

您还可以通过按住 SHIFT 键执行某些命令，将命令响应重定向到该窗口，以避免在不希望编辑代码时编辑它。

您可以在菜单 "View -> Other Windows -> Visual ChatGPT Studio" 中找到这个窗口。

## "Visual ChatGPT Studio Turbo" 工具窗口的功能 🚀

在这个窗口的编辑器中，您可以直接与 ChatGPT 进行交互，就像您在 ChatGPT 门户中一样：

与之前的窗口不同，在这个窗口中，AI “记住”了整个对话：

![image](https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/ff47bf5c-8324-46ba-a039-173c172337e0)

您还可以通过“发送代码”按钮与已打开的代码编辑器进行交互。使用此按钮，OpenAI API 会了解已打开编辑器中的所有代码，并且您可以直接向您的代码请求互动，例如：

- 在特定行添加新的方法，或者在两个现有方法之间添加；
- 更改现有方法以添加新的参数；
- 询问该类是否存在任何 bug；
- 等等。

但要注意，因为这会将打开文件的整个代码发送到 API，这可能会增加令牌的消耗。而且，根据您使用的模型，可能会更快地达到每个请求的令牌限制。设置一个具有更大令牌限制的模型可以解决这个限制。

通过执行这个命令，您还可以按住 SHIFT 键，然后点击 "发送代码" 按钮，这样代码将直接写入聊天窗口，而不是代码编辑器中，以便在应用到已打开的代码编辑器之前保留原始代码和/或分析响应。

您可以在菜单中找到这个窗口：View（视图） -> Other Windows（其他窗口） -> Visual chatGPT Studio Turbo（视觉 chatGPT Studio Turbo）。

请看这里一些使用 Turbo Chat 的示例：

您可以在菜单 "查看"->"其他窗口"->"Visual chatGPT Studio Turbo "中找到该窗口。

在这里观看一些使用 Turbo Chat 的例子：

[<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/275615943-a37e30c3-d597-42de-8a38-7d0cdbfe942f.png" width="70%">](https://youtu.be/2NHWWXFMpd0)

## "Visual chatGPT Studio Solution Context" 工具窗口的特性 📌

<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/282463640-4e7c24c6-41d5-4cee-aa36-f363daba6f95.png" width="75%">

在这里，您可以将项目项添加到与 OpenAI 请求上下文相关的工具窗口中。非常适合发出需要了解项目其他点的请求。

例如，您可以请求在当前文档中创建一个方法，该方法调用通过此窗口选择的另一个类中的方法。

您还可以要求在通过上下文引用的另一个类的打开文档中创建单元测试。

您还可以请求涉及涉及多个类的更大上下文的分析。可能性很多。

但请注意，根据您添加到上下文的代码量，这可能会增加 tokens 的消耗。而且，根据您使用的模型型号，可能更早达到每个请求的 tokens 上下文数量限制。选择具有较大 tokens 上下文限制的模型可以解决此限制。

您将在菜单 "View" -> "Other Windows" -> "Visual chatGPT Studio Solution Context" 中找到此窗口。

## 认证 🔑

要使用此工具，必须通过 OpenAI API 或 Azure OpenAI 进行连接。

### By OpenAI

1 - 在 OpenAI 上创建一个账户: https://platform.openai.com

2 - 生成一个新 KEY : https://platform.openai.com/api-keys

3 - 复制并粘贴KEY到选项中，将 "OpenAI 服务" 参数设置为 "OpenAI"：

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/09a93cc9-c35d-4fee-b3a1-05f2dd0212f1" width="75%">

4 - 如果想使用第三方中转API,可以修改 "Base API " 参数，用来覆盖默认的 URL。

### By Azure

1 - 首先，您需要访问 Azure OpenAI 服务。您可以在这里查看更多详细信息。 [here](https://learn.microsoft.com/en-us/legal/cognitive-services/openai/limited-access?context=%2Fazure%2Fcognitive-services%2Fopenai%2Fcontext%2Fcontext).

2 - 创建一个 Azure OpenAI 资源，并在选项中设置资源名称。示例：

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/8bf9111b-cc4d-46ac-a4f2-094e83922d95" width="60%">

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/1e9495a7-d626-4845-af7f-ae6f84139d87" width="75%">

3 - 复制并粘贴选项上的KEY，将 "OpenAI 服务 "参数设置为 "AzureOpenAI"：

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/2f881df1-a95f-4016-bf39-9cf2e83aef0e" width="75%">

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/8b035735-ce8e-4f25-a42e-6a0d14058c98" width="75%">

4 - 通过 Azure OpenAI Studio 创建一个新的部署，并设置名称：

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/3914ddf3-e0c5-4edd-9add-dab5aba12aa9" width="40%">

<img src="https://github.com/jeffdapaz/VisualChatGPTStudio/assets/63928228/195539ac-8d0b-4284-bac4-de345464ed08" width="75%">

5 - 设置 Azure OpenAI API 版本。您可以检查可用的版本。 [here](https://learn.microsoft.com/en-us/azure/ai-services/openai/reference#completions).

## 已知问题 ⚠

很遗憾，OpenAI提供的用于与chatGPT交互的API对于问题及其给定答案的长度有一定限制。

如果发送的问题太长（例如，一个包含许多行的方法），和/或生成的响应太长，API可能会截断响应，甚至可能完全不回应。

针对这些情况，我建议您通过工具窗口进行请求，以定制问题的方式，使chatGPT不会拒绝回答，或尝试修改模型选项以改善响应。

## 免责声明 👋

由于此扩展依赖于由OpenAI提供的API，可能会有一些由于他们的更改而影响此扩展操作的情况，恕不另行通知。

由于此扩展依赖于由OpenAI提供的API，可能会生成一些与预期不符的响应。

响应的速度和可用性直接取决于OpenAI提供的API。

如果您使用的是OpenAI服务而非Azure，并收到类似于 "429 - 您已超出当前配额，请检查您的计划和计费详情" 的消息，请检查OpenAI使用页面，看看您是否仍然有配额，例如：

<img src="https://user-images.githubusercontent.com/63928228/242688025-47ec893e-401f-4edb-92a0-127a47a952fe.png" width="60%">

您可以在此处检查您的配额使用情况: [https://platform.openai.com/account/usage](https://platform.openai.com/account/usage)

- 如果发现任何错误或意外行为，请留下评论，以便我提供修复。

## 捐赠 🙏

☕ 请我喝杯咖啡，支持我为您提供更多帮助。谢谢！

[<img src="https://github-production-user-asset-6210df.s3.amazonaws.com/63928228/278758680-f5fc9df2-a330-4d6a-ae13-9190b7b8f57b.png" width="20%">](https://www.paypal.com/donate/?hosted_button_id=2Y55G8YYC6Q3E)

## 发布说明 📜

### 2.4.4

- 在某些写有代码编辑器中的注释的命令中添加注释字符方面进行了小的改进。
- 在聊天标题的自动创建方面进行了小的改进。

### 2.4.3

- 添加了 gpt-4-32k 模型。
- 移除了 gpt-3.5-turbo-16k 模型。OpenAI 目前指向 gpt-3.5-turbo 模型。
- 修复了导致对 API 发送重复请求的错误。
- 改进了对与连接相关选项的更改的验证，以在执行模式中应用这些更改，避免需要重新启动 Visual Studio 以生效。

### 2.4.2

- 在工具窗口上添加了换行开关按钮。
- 添加了在上下文限制达到时自动从聊天历史中删除早期聊天消息的功能。

### 2.4.1

- 修复了在上次发布中尝试使用 Turbo 聊天窗口时引入的错误。该错误与创建存储聊天历史的文件有关。

### 2.4.0

- 在 Turbo 聊天中添加了选项卡。
- 添加了聊天历史记录。
- 修复了打开其中一个扩展工具窗口时导致打开所有扩展工具窗口的行为。

### 2.3.0

- 添加了新的模型 gpt-3.5-turbo-1106 和 gpt-4-1106-preview（可能尚未与 Azure 兼容）。有关这些新模型的更多详情，请参阅[这里](https://openai.com/blog/new-models-and-developer-products-announced-at-devday)。
- 添加了新选项 "Log Request" 和 "Log Responses"。如果打开，所有发送到 OpenAI 的请求和/或响应将被记录到输出窗口。
- 重新制定了命令选项。现在可以按项目和解决方案设置命令。
- 添加了新的 "Visual chatGPT Studio Solution Context" 窗口，可以在其中将项目中的项目添加到发送到 OpenAI 的请求的上下文中。
- 修复了一个 bug，在某些情况下未能显示错误消息，导致与 OpenAI 通信失败。结果，请求的执行没有中断。

### 2.2.2

- 添加了在执行 "Send Code" 命令时按住 SHIFT 键将代码写入 Turbo Chat 窗口而不是代码编辑器的可能性。
- 将选项中的 Turbo Chat Code Command 从 "将用户要求的更改应用于代码 "更改为 "将用户要求的更改应用于代码，但重写未更改的原始代码"，以获得更好的性能。

### 2.2.1

- 对请求添加了超时。

### 2.2.0

- 在 Turbo Chat 窗口中添加了 "Send Code" 命令。

### 2.1.0

- 添加了 "Cancel" 命令以停止接收/等待请求。

### 2.0.0

- 移除了Completion模型。现在所有请求都将通过Chat Models进行。
- 调整了Azure选项的参数，因为不再需要两个资源。
- 进行了一些小的重构和修复。

### 1.13.0

- 新增了选项 "Minify Requests"。如果设置为 true，所有发送给 OpenAI 的请求都将被压缩，以节省令牌（Tokens）。
- 新增了选项 "Characters To Remove From Requests"。可以添加需要从发送给 OpenAI 的所有请求中移除的字符或词语。它们必须用逗号分隔，例如："a,1,TODO:,{"。
- 现在进度状态将显示在工具窗口本身。
- 修复了当类中包含 "region" 标签时 "Add Summary For Entire Class" 命令的问题。
- 修复了 "Add Comments" 命令，以正确检测是否选择了一行或多行代码。

### 1.12.1

- 对“为整个类添加摘要”的命令进行了轻微修正。
- 在显示“优化（差异视图）”命令的差异视图时，差异视图上的文件扩展名将根据原始文件扩展名显示。

### 1.12.0

- 增加了新命令 "Optimize (Diff View)"。
- 对于 "Add Summary For Entire Class" 命令进行了小修复，在某些情况下 chatGPT 的回复不佳时，类的代码会被清除。
- 添加了中文的自述文件。感谢 [ATFmxyl](https://github.com/ATFmxyl) 的合作！

### 1.11.2

- 更新为在执行 "为整个类添加摘要 "命令时为构造函数成员添加摘要。

### 1.11.1

- 更新，在执行命令“为整个类添加摘要”时，为更多的类成员添加摘要。

### 1.11.0

- 增加了新命令：“为整个类添加摘要”。
- 为 Turbo 聊天窗口添加了 gpt-3.5-turbo-16k 模型。
- 增加了“基础 API”选项。

### 1.10.0

- 增加了新命令：“翻译”。
- 对选项进行了分组，以提高可用性。
- 在工具窗口复制代码时添加了反馈。

### 1.9.3

- 增加了选项，允许定义 Turbo 聊天窗口的特定 Azure 部署 ID。
- 增加了选项，允许定义 Turbo 聊天窗口的 Azure API 版本。

### 1.9.2

- 移除了“一个方法选择”限制。

### 1.9.1

- 为 Visual Studio 2019 版本更改了扩展图标。

### 1.9.0

- 添加了 Visual Studio 2019 兼容性。感谢 [przemsiedl](https://github.com/przemsiedl)。
- 修复了代理连接(实验性)。感谢 [52017126](https://github.com/52017126)。
- 添加了通过 Azure 连接的可能性(实验性)。感谢 [Rabosa616](https://github.com/Rabosa616)。  
- OpenAI 已弃用 CodeDavinci 和 CodeCushman 模型,因此也将其删除。感谢 [ekomsctr](https://github.com/ekomsctr) 提供此反馈。
- 为“解释”和“查找错误”命令修复了非 C# 语言的注释前缀。 
- 添加了定义 OpenAI 组织的选项。
- 为 TurboChat 窗口添加了 GTP-4 模型语言(实验性)。
- 在将代码复制到 TurboChat 窗口时添加了反馈。

### 1.8.0

- 增加了代理连接（实验性功能）。感谢 [SundayCoding](https://github.com/SundayCoding)。

### 1.7.5

- 对“添加摘要”命令进行了改进。

### 1.7.4

- 对 Turbo 聊天窗口进行了一些改进。
- 增加了新的选项参数“Single Response”。如果设置为 true，则整个响应将一次性显示在代码编辑器中。撤销历史较少，但等待时间较长。

### 1.7.3

- 对 Turbo 聊天窗口进行了改进。添加了语法高亮、垂直滚动和复制按钮。
- 改进了 TSQL 语法检测。
- 现在，当按住 SHIFT 键执行上下文菜单命令时，所进行的请求也会写入请求框中。

### 1.7.2

- 修复了一个在所选代码包含一些特殊字符如 '<' and '>' 时的错误。

### 1.7.1

- 为上下文菜单项添加了图标。
- 改进了 Api Token 验证。现在，在首次设置令牌后无需重新启动 Visual Studio，同时避免了相关错误。

### 1.7.0

- 增加了新的“Visual chatGPT Studio Turbo”工具窗口。
- 有时在执行“添加摘要”命令时，API 最终会在响应中添加字符“{”和/或“}”。因此，当这些字符作为该命令的结果返回时，我会从响应中删除它们。
- 新增了定制命令 Before、After 和 Replace。
- 在选项中添加了“停止序列”选项（感谢 [graham83](https://github.com/graham83)）。

### 1.6.1

- 修复了在上一个版本引入的“解释”、“添加注释”和“添加摘要”命令中的错误。

### 1.6.0

- 由于不兼容性，移除了对 Visual Studio 2019 的提及。
- 为“解释”和“查找错误”命令添加了注释前缀。
- 为“解释”和“查找错误”命令在同一行上超过 160 个字符后添加了换行。
- 当 API 仅在响应开头发送换行时，不执行任何操作（避免新的空行）。
- 将命令移动到子菜单中。
- 为命令添加了快捷键。
- 修复了在某些情况下使用 SHIFT 键执行命令时的错误。

### 1.5.1

- 将命令响应重定向到工具窗口。要执行此操作，按住 SHIFT 键并选择命令。响应将被写入工具窗口，而不是代码编辑器中。

### 1.5.0

- 为工具窗口中的文本编辑器添加了语法高亮。
- 现在文本编辑器显示行号。

### 1.4.0

- 增加了通过选项自定义命令的可能性。
- 增加了在工具窗口上调整文本编辑器大小的可能性。
- 为工具窗口上的“请求”按钮添加了快捷键。现在，您只需按下 CTRL+Enter 即可发送请求。

### 1.3.2

- 现在扩展在出现 OpenAI API 错误时将显示详细信息。这样您可以了解到实际发生了什么：

![image](https://user-images.githubusercontent.com/63928228/223844744-e7a9b350-b590-40c6-a36a-6ce8d327eb9f.png)

### 1.3.1

- 增加了对 Visual Studio ARM 架构的支持。

### 1.3.0

- 改进了“添加注释”命令。

- 现在可以自定义 OpenAI API 请求参数：

![image](https://user-images.githubusercontent.com/63928228/223844877-d11a524b-472d-4046-94c5-70e6d3a49752.png)

但仅在您知道自己在做什么的情况下才更改默认值，否则可能会遇到一些意外行为。

有关每个属性的更多详细信息，请参阅 OpenAI 文档：[https://platform.openai.com/docs/api-reference/completions/create](https://platform.openai.com/docs/api-reference/completions/create)。

在在插件中更改这些参数之前，您可以在此处进行参数调试：[https://platform.openai.com/playground](https://platform.openai.com/playground)。

### 1.2.3

- 修复了从底部到顶部或从末尾到开头选择代码时的命令问题（感谢 Tim Yen 提醒我关于这个问题）。
- 在“摘要”命令上进行了更多改进。
- 修复了一些小错误。

### 1.2.2

- 改进了“摘要”命令。

### 1.2.1

- 改进了“摘要”命令。现在大多数情况下，ChatGPT 不会再次写入方法/属性头部。
- 添加了验证，以避免 ChatGPT 的响应被写入错误的行。
- 其他小修复。

### 1.2.0

- 添加了新的命令“Complete”。开始编写一个方法，选择它，然后请求完成。
- 现在该扩展与 Visual Studio 2019 兼容。
- 修复了一些小错误。

### 1.1.0

- 我找到了一种改进请求大小的方法。这将解决大多数“发生了一些错误。请重试或更改选择。”错误。现在，在大多数情况下，如果超过了数据限制，ChatGPT 将不会写入整个响应，而不是失败。

### 1.0.1 

- 修复了一个在关闭并重新打开 Visual Studio 后阻止窗口工具工作的错误。
- 在扩展等待 ChatGPT 响应时添加了反馈：

![image](https://user-images.githubusercontent.com/63928228/223844933-2e6775d6-c657-42a1-9417-111aa3fb3245.png)
