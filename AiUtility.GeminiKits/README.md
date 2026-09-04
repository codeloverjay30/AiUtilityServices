# Description
`Gemini` kit, which is used for Gemini Agent.

# Features
## 1.0.2-preview-1.0.0
### Added
+ `GeminiKit` Tool kit, which is used for Gemini Agent.

## 1.0.6-preview-1.0.0
### Update
+ Use interface instead.

## 3.0.0-preview-1.0.0
### Major Updates
+ Rename namespace

## 4.0.0-preview-1.0.0
### Major Updates
+ Check the argument type and set in tool dispatcher (`AiToolDispatcher`,`GeminiToolDispatcher`)

### Removed
+ Remove unneccessary code (code with same functionality) 

    - `GeminiToolExecutor`

### API
| `old` | `replaced` |
| :---- | :---- |
| `AiToolExecutor` | `AiToolDispatcher` |
| `ExecuteAsync` of `AiToolExecutor` | `DispatchAsync` of `AiToolDispatcher`|
| `GeminiToolExecutor` | `GeminiToolDispatcher`|
| `ExecuteAsync` of `GeminiToolExecutor` | `DispatchAsync` of `GeminiToolDispatcher`|


## 4.0.0-preview-1.0.1
### Major Updates
+ recompile the file and repack the utility packages
