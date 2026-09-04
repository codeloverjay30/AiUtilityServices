# Description
Utility class about API of AI, specially for API of Gemini AI Studio.

For developer, one can easily integrate Gemini AI Studio into its developing tools (or projects)

# Prequisites
To call API of Gemini AI Studio, one needs

+ API key of Gemini AI Studio

# Features
## 1.0.0-preview-1.0.0
### Added
One can interact with Gemini AI Studio. The following available interactions are

+ add text (as prompt) or image as a part.

+ send message to call API of Gemini AI Studio

Additionally, it can automatically do following things before or after AI gives a respone. 

+ consolidate the token (by summarize the chat or purge the old media) so that the AI Model forgot the previous interactions due to token overflows the available context window

+ register the service by Attrbute of Data Annotations.

+ execute the task using the registered service given AI response.

For developer (who install this package), one also can easily do following things by a method call.

+ load session from file (it is useful when transfer from one chat to others)

+ save session (including the contents of chat) to a file.

To make the maintenance (one who develops and maintains this package) or

developing (one who installs this packages and develops other solution) more easily,

the maintainer defines the constants (instead of hard-code) and defines the POCO for the schema used for Gemini AI Studio.

## 1.0.3-preview-1.0.0
### Fixed
+ Add public getter-properties of `GeminiAgentService` class to make the other project can invoke its API call. 

## 1.0.4-preview-1.0.0
### Fixed
+ Define public getter-properties of `IGeminiAgentService` interface to make the other project can invoke its API call. 

## 1.0.5-preview-1.0.0
### Fixed
+ Define public getter-properties of `GeminiAgentService` class and `IGeminiAgentService` interface to make the other project can invoke its API call. 

## 1.0.6-preview-1.0.0
### Fixed
+ Define public getter-properties of classes and interfaces to make the other project can invoke its API call. 

+ Define properties of interfaces (that implemented by classes)

## 2.0.0-preview-1.0.0
### Fixed
+ Define public getter-properties of classes and interfaces to make the other project can invoke its API call. 

+ Define properties of interfaces (that implemented by classes)

## 2.1.0-preview-1.0.0
### Added
+ Set the `prompt`

### Updated
+ For some API, check arguments first.

### Changed
+ For some API, it can determine to keep the last n token according to the settings `AiExecutionSetting`

## 2.2.0-preview-1.0.0
### Added
+ Clone a POCO

## 3.0.0-preview-1.0.0
### Refactored Performance
+ For better performance, use `ReadyOnlyMemory<char>` instead of `string` and `ReadOnly<byte>` instead of `byte []`

+ In POCO, add backend getter-setter property to convert the `ReadyOnlyMemory<char>` instance to `string` (and vice versa)

and convert `ReadyOnlyMemory<byte>` instance to `string` (and vice versa).

### Major Updates
+ Update some legacy packages.

+ give alias for packages that has same namespace.

### Updated
+ replace harded-code with constants.

## 4.0.0-preview-1.0.0
### Major Updates
+ Remove unneccesary proerties (e.g. `ConfigPath` in `AiBaseAbstractService`)

## 4.0.0-preview-1.0.1
### Major Updates
+ recompile the file and repack the utility packages

## 4.1.0-preview-1.0.0
### Added
+ Add `GeminiToolExecutor` executor as adapter.

## 4.1.1-preview-1.0.0
### Fixed
+ 0 應該是合法的初始 token state