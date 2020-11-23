# Part 3 - View Components - CRUD Edit and View Operations in the UI

# Introduction

This is the third in a series of articles looking at how to build and structure a real Database Application in Blazor. The articles so far are:

- [Project Structure and Framework](https://www.codeproject.com/Articles/5279560/Building-a-Database-Application-in-Blazor-Part-1-P)
- [Services - Building the CRUD Data Layers](https://www.codeproject.com/Articles/5279596/Building-a-Database-Application-in-Blazor-Part-2-S)

This article looks in detail at building reusable CRUD presentation layer components, specifically Edit and View functionality - and using them in Server and WASM projects.


## Sample Project and Code

The UI is more radical:

1. A custom `Component` class is used as the base UI component - `ControlBase` is only used by the out-of-the-box data controls.
2. Routing is discarded.  There's a new `ViewManager` that manages the UI.
1. A custom `Component` class is used as the base UI component - `ControlBase` is only used by the out-of-the-box data controls.
2. Routing is discarded.  There's a new `ViewManager` that manages the UI.

![Project Files](https://github.com/ShaunCurtis/CEC-Publish/blob/master/Images/CEC.Blazor.WASM.Client-2.png?raw=true)

   
```c#
// CEC.Blazor/Components/Base/BaseForm.cs
private IServiceScope _scope;

/// Scope Factory to manage Scoped Services
[Inject] protected IServiceScopeFactory ScopeFactory { get; set; } = default!;

/// Gets the scoped IServiceProvider that is associated with this component.
protected IServiceProvider ScopedServices
{
    get
    {
        if (ScopeFactory == null) throw new InvalidOperationException("Services cannot be accessed before the component is initialized.");
        if (IsDisposed) throw new ObjectDisposedException(GetType().Name);
        _scope ??= ScopeFactory.CreateScope();
        return _scope.ServiceProvider;
    }
}
```

Blah Blah `Blah`.


> dddd 
> eeeee 

## History

- 19-Sep-2020: Initial version.
- 17-Nov-2020: Major **Blazor.CEC** library changes.  Change to ViewManager from Router and new Component base implementation.
