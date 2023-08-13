# Visual chatGPT Studio  

[English ](README.md)| Chinese

这是一个可以在 Visual Studio 中直接添加 chatGPT 功能的扩展。

你可以通过文本编辑器或者一个新的特定的工具窗口直接咨询 chatGPT。 

[这里观看一些示例。](https://www.youtube.com/watch?v=h_wUl_IjWRU)

- 对于 Visual Studio 2022:[这里](https://marketplace.visualstudio.com/items?itemName=jefferson-pires.VisualChatGPTStudio)  
- 对于 Visual Studio 2019:[这里](https://marketplace.visualstudio.com/items?itemName=jefferson-pires.VisualChatGPTStudio2019)

## 文本编辑器中的功能

选择一个方法并在文本编辑器上右键单击,你会看到这些新的 chatGPT 命令:

![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio2019/1.11.0/1691094951157/image__1.png)

- **完成:** 开始编写一个方法,选择它并请求完成。 
- **添加测试:** 为所选方法创建单元测试。
- **找到错误:** 为所选方法找到错误。 
- **优化:** 优化所选方法。
- **解释:** 为所选方法编写说明。
- **添加注释:** 为所选方法添加注释。
- **添加摘要:** 为C#方法添加摘要。
- **为整个类添加摘要:** 为整个C#类添加摘要(适用于方法、属性、枚举、接口、类等)。不需要选择代码,只需运行命令即可启动该过程。
- **问任何问题:** 在代码编辑器上写下一个问题并等待答案。  
- **翻译:** 用翻译后的文本替换所选文本。在选项窗口中编辑命令,如果你想翻译成除英语外的其他语言。
- **自定义之前:** 通过选项创建一个自定义命令,在所选代码之前插入响应。 
- **自定义之后:** 通过选项创建一个自定义命令,在所选代码之后插入响应。
- **自定义替换:** 通过选项创建一个自定义命令,用响应替换所选文本。

如果你希望响应写入工具窗口而不是代码编辑器,请按住 SHIFT 键并选择命令(快捷键不起作用)。

如果你想要 chatGPT 以另一种语言回复和/或由于某些原因要自定义命令,你可以通过选项编辑默认命令:

![图片](https://user-images.githubusercontent.com/63928228/226494626-d422a843-2512-4dee-a177-045f39c0b6d3.png)

例如,通过将“解释”命令的“解释”提示更改为“用西班牙语解释”(或只是“解释”),OpenAI API 将用西班牙语而不是默认的英语命令编写注释。

## “Visual chatGPT Studio”工具窗口中的功能

在此工具窗口中,你可以向 chatGPT 提出问题并直接在其中接收答案。

此窗口中的交互使用此扩展的选项中定义的参数(以及代码编辑器中的命令): 

![图片](https://user-images.githubusercontent.com/63928228/225486306-d29b1ec3-2ccd-4d74-8153-806a84abe5ea.png)

你可以在菜单查看->其他窗口-> Visual chatGPT Studio 中找到此窗口。

## “Visual chatGPT Studio Turbo”工具窗口中的功能

在这个新的窗口编辑器中,你可以像在 chatGPT 门户本身中一样直接与 chatGPT 进行交互:

与前一个窗口不同,在这个窗口中,AI“记住”了整个对话,甚至可以通过选项参数化一个人格:

![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio/1.7.1/1679515374543/image__11.png)

![image](https://jefferson-pires.gallerycdn.vsassets.io/extensions/jefferson-pires/visualchatgptstudio/1.7.3/1679698833202/image__14.png)

虽然第一个工具窗口使用了特定的语言模型用于完成,但这个窗口使用了特定的聊天模型,即 GPT-3.5-Turbo 和 GPT-4,可以通过选项进行选择。

请注意,GPT-4 当前并非对所有人开放。更多详细信息请参阅[此处](https://openai.com/blog/gpt-4-api-general-availability)。

你可以在菜单查看->其他窗口-> Visual chatGPT Studio Turbo 中找到此窗口。  

## 认证

使用此工具需要通过 OpenAI API 或 Azure OpenAI 进行连接。

### 通过 OpenAI  

你需要创建一个 OpenAI API 密钥并进行设置。

你可以在此处创建: [https://beta.openai.com/account/api-keys](https://beta.openai.com/account/api-keys)

### 通过 Azure

更多详细信息请参阅:[https://learn.microsoft.com/azure/cognitive-services/openai/overview](https://learn.microsoft.com/azure/cognitive-services/openai/overview)。

你还需要在 Azure 上创建两个资源,一个用于 Turbo 窗口,另一个用于菜单命令和其他工具窗口。更多详细信息请阅读[此处](https://github.com/jeffdapaz/VisualChatGPTStudio/issues/31)。

## 已知问题

不幸的是,OpenAI 为与 chatGPT 交互提供的 API 对问题加答案的大小有限制。

如果发送的问题太长(例如,有许多行的方法)和/或生成的响应太长,API 可能会截断响应或者根本不响应。

对于这些情况,我建议你通过工具窗口提出请求,以便以 chatGPT 不会拒绝回答的方式自定义问题,或者尝试修改模型选项以改善响应。

## 免责声明

- 由于此扩展取决于 OpenAI 提供的 API,他们可能会在未事先通知的情况下对此扩展的操作进行一些更改。

- 由于此扩展取决于 OpenAI 提供的 API,可能会生成与预期不符的响应。

- 对响应的速度和可用性直接取决于 OpenAI 提供的 API。

- 如果你使用 OpenAI 服务而不是 Azure 并收到类似“429 - 您超过了当前配额,请检查您的计划和账单详细信息。”的消息,请检查 OpenAI 使用页面并查看您是否还有配额,例如:

![image](https://user-images.githubusercontent.com/63928228/242688025-47ec893e-401f-4edb-92a0-127a47a952fe.png)

您可以在此处检查您的配额:[https://platform.openai.com/account/usage](https://platform.openai.com/account/usage)

- 如果您发现任何错误或异常行为,请留言以便我提供修复。

## 发行说明

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
