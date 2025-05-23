

[![NuGet Version](https://img.shields.io/nuget/v/ExIgniter.ObjectMapper.svg?style=flat-square)](https://www.nuget.org/packages/ExIgniter.ObjectMapper/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
# ExIgniter.ObjectMapper 2.0

&#x20;

**ExIgniter.ObjectMapper** is an intelligent, high-performance object mapping library for .NET.

Version 2.0 delivers:

* 🚀 Drastically improved performance
* 🔐 Safe mapping with circular reference detection
* 🧠 Smart property matching
* 🧱 Deep support for collections and complex graphs

---

## ✨ Why ExIgniter?

* ✅ Zero-configuration for 90% of use cases
* ✅ 3x faster than v1.0 in benchmarks
* ✅ Automatically matches similar property names
* ✅ Safely maps nested objects, collections, and dictionaries
* ✅ Security-first: detects circular references, restricts unsafe types, and limits recursion depth

---

## 📦 Installation

```bash
Install-Package ExIgniter.ObjectMapper
```

Supports: `.NET Standard 2.1+`, `.NET 6+`, `.NET 7+`

---

## 🔑 Key Features

### 🚀 Performance Optimized

* Reflection caching
* Lazy initialization
* Minimal allocations

### 🧠 Intelligent Mapping

Automatically resolves common mismatches:

```csharp
"UserName" → "Username"
"Addr1" → "AddressLine1"
"ID" → "Id"
```

### 🔐 Safe and Secure

* Cycle detection using object graph tracking
* Max depth limit to prevent runaway recursion
* Whitelisted types to avoid instantiating unsafe types

### 🧰 Collection Support

* Arrays, Lists, HashSets
* Dictionaries
* Queues and Stacks

---

## 📊 Benchmarks (v1.0 vs v2.0)

| Scenario        | v1.0  | v2.0  | Speedup |
| --------------- | ----- | ----- | ------- |
| Simple Object   | 150ms | 50ms  | 3×      |
| Complex Graph   | 420ms | 140ms | 3×      |
| Collection (1k) | 220ms | 70ms  | 3.1×    |

*Benchmarks were performed using representative DTOs and nested entities.*

---

## 📚 Documentation

* Full API reference
* Configuration guide
* Migration steps from v1.x to v2.0

🔗 [View Docs](https://github.com/yourname/ExIgniter.ObjectMapper/wiki)

---

## 🤝 Contributing

We welcome contributions! Please see the [Contribution Guidelines](https://github.com/yourname/ExIgniter.ObjectMapper/blob/main/CONTRIBUTING.md).

---

## 📄 License

MIT License — free for commercial and personal use.

---

Happy mapping! 🎯
