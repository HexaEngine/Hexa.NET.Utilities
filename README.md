# Hexa.NET.Utilities
This repository provides a collection of high-performance utilities designed to address common programming needs while maintaining low GC pressure and maximizing performance.

## Features

### UTF-8 String formatting
- **Utf8Formatter**
  - A low level implementation for maximum performance. 
  - **Number primitives** incl. short ushort int uint long ulong float double and culture specifics
  - **Hexadecimal formatting**
  - **Data size formatting** eg. 1024 => 1 KiB.
  - **DateTimes** full format string support and culture specifics
  - **TimeSpans** full format string support and culture specifics
- **`StrBuilder`**:
  - a lightweight string builder allowing use of stackalloc for temporary strings to prevent GC-Pressure.

### IO Utilities
- **Faster File System Enumeration with prefetching metadata**

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

This project is licensed under the MIT License. See the [LICENSE](LICENSE.txt) file for more details.
