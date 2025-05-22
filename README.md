# ExIgniter.ObjectMapper

A fast and flexible object-to-object mapping library for .NET, built for performance and deep object graph support. Ideal for mapping between DTOs, ViewModels, and domain entities.

---

## 🚀 Features

* 🔄 Deep mapping of nested complex types
* 🔍 Similarity-based property matching (name similarity, configurable)
* 🔁 Collection mapping (e.g., `List<T>`, `IEnumerable<T>`)
* ❌ Exclude properties with a lambda function
* ⚡ High-performance via property caching
* ✅ Backward-compatible APIs: `FasterMap`, `ComplexMap`, `Map`

---

## 🛠️ Installation

```bash
Install-Package ExIgniter.ObjectMapper
```

---

## 🧪 Usage

### Basic Mapping

```csharp
var result = source.Map<DestinationType>();
```

### Exclude Specific Properties

```csharp
var result = source.Map<DestinationType>(x => new[] { "IgnoreThisProp" });
```

### Map Collections

```csharp
List<SourceType> sources = GetSources();
var targets = sources.Map(new List<DestinationType>());
```

### Backward-Compatible Methods

```csharp
source.FasterMap(destination);
source.ComplexMap(destination);
```

---

## ✅ Supported Types

### ✔️ Primitive Types

* `int`, `string`, `bool`, `decimal`, `float`, `double`, `DateTime`, `Guid`, `TimeSpan`, etc.

### ✔️ Complex Types

* Nested classes
* Lists and collections

### ❗ Unsupported (for now)

* Dictionaries
* Custom converters (planned)

---

## 🧪 Running Tests

Unit tests are written using [xUnit](https://xunit.net):

```bash
dotnet test
```

Tested scenarios:

* Mapping all primitive types
* Mapping nested and complex objects
* Mapping object collections

---

## 📦 Contributions

PRs are welcome! Please add tests for any new features or bug fixes.

---

## 📄 License

MIT License
