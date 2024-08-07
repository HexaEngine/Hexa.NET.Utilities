# HexaEngine Utilities Library

The Utilities library for HexaEngine provides a set of essential tools and utilities that enhance the functionality and performance of your applications. It includes robust data structures, memory management utilities, thread-safe components, and more.
The library is tailored to HexaEngine, but can be still used in other projects, that require low GC Pressure.

## Features

### Data Structures
- **Standard-like Strings**:
  - `StdWString` (UTF-16)
  - `StdString` (UTF-8)
- **Standard-like Containers**:
  - `List` (UnsafeList)
  - `Map` (UnsafeDictionary)
  - `Set` (UnsafeHashSet)
  - `Queue` (UnsafeQueue)
  - `Stack` (UnsafeStack)

### Memory Management
- **Custom Allocation Callbacks**: Define your own memory allocation strategies.
- **Pointer Wrapper Types**: Utilize with generics for safer and more efficient pointer operations.
- **Utility Functions**:
  - Memory allocation, freeing, copying, and moving
  - String operations
  - Memory setting (e.g., `Memset`)
  - Sorting (e.g., `QSort`)

### Thread Safety
- **Thread-Safe Pools**:
  - Object pools
  - List pools

## Getting Started

To get started with the HexaEngine Utilities library, follow these steps:

1. **Install the NuGet package**:
    ```bash
    dotnet add package Hexa.NET.Utilities
    ```

2. **Include the library in your project**:
    ```csharp
    using Hexa.NET.Utilities;
    ```

3. **Initialize and utilize data structures**:
    ```csharp
    var myString = new StdString("Hello, HexaEngine!");
    var myList = new UnsafeList<int> { 1, 2 };
    ```

4. **Leverage memory management utilities**:
    ```csharp
    int* memory = Utils.AllocT<int>(1);
    Utils.Free(memory);
    ```
    or
   ```csharp
    global using static Hexa.NET.Utilities.Utils;
  
    int* memory = AllocT<int>(1);
    Free(memory);
   ```

6. **Use thread-safe components for concurrent operations**:
    ```csharp
    var pool = new ObjectPool<MyObject>();
    var obj = pool.Rent();
    pool.Return(obj);
    ```

## Contributions

Contributions are welcome! If you have ideas for new features or improvements, feel free to submit a pull request or open an issue.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.
