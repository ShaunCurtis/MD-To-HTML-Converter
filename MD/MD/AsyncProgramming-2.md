# Understanding and Using Async Programming in DotNetCore and Blazor - Part 2

The first article provided an overview of Async Programming in DotNetCore and explained some of the key concepts.  This article uses a program titled "A morning in the life of an out-of-work programmer" for a discussion on:
1. How it's built.
2. The Async principles used.
3. Code tweating to show how subtle changes have big impacts on how it runs.

It's not a step-by-step description of how to build the application.  Grab the Repo and look therough the code for that

> The code is **Experimental** rather that **Prodiction**.  It's concise, with minimal confusing error trapping clutter. Concept implementations are kept as simple as possible.

## This Morning's Routine for that out-of-work Programmer

The pseudo code template looks like this:

- Get Started
- Have Breakfast
- Do the washing up - everyone else went to work or school so the sink is full.
- Do the Laundry - the basket is full.
- Go to the Shops if there's anything on the shopping list (anything to get out and about)
- Do the hoovering\vacuuming
- Put you feet up

## Code Repositories and Running Sites


## The Console Version

First, we'll look at the console application.  It's quick and easy to run and debug and we can concentrate on the core code rather than what's happening in the UI.

### Setting

The code is split into two projects:
1. The console application
2. A library for the code shared between the console application and the web application.
   
Before we dive into the main code let's look at some of the infrastructure in the code:

#### BaseClass

`BaseClass` provides common functionality.  This is:
1. Logging of information.
2. Simulated long running Tasks.

The Logging implementation is simplistic - Experimental rather than Production.

A logged line is in the format *[Thread name][Task Name] > message* so we can see which tasks on running on which threads.  An example:     

```text
[Main Thread][Morning Chores] > Morning - what's on the agenda today!
```

All classes that need to output messages inherit from `BaseClass` which defines an Action Delegate `UIMessenger` and several logging methods. It's up to the class consumer to assign an `Action<string>` to the consumer.

```c#
public abstract class BaseClass
{
    public Action<string> UIMessenger;

    protected string CallerName = "Application";

    protected void LogToUI(string message, string caller = null)
    {
        caller ??= this.CallerName;
        if (string.IsNullOrWhiteSpace(Thread.CurrentThread.Name))
            Thread.CurrentThread.Name = $"{caller} Thread";
        message = $"[{Thread.CurrentThread.Name}][{caller}] > {message}";
        UIMessenger?.Invoke(message);
    }

    protected void WriteDirectToUI(string message) =>
        UIMessenger?.Invoke(message);

    protected void LogThreadType(string caller = null)
    {
        caller ??= this.CallerName;
        var threadname = Thread.CurrentThread.IsThreadPoolThread ? "Threadpool Thread" : "Application Thread";
        this.LogToUI($" running on {threadname}", caller);
    }
}
```
When one of the logging methods is run it calls `UIMessenger?.Invoke(message)`.  Note the `?`.  `Invoke` is only called if `UIMessenger` is assigned an Action.  The message is only passed on if it has an Action to pass it on to.


`Program` defines `LogToConsole` - a simple `Action<string>` pattern method that writes to the console.
```c#
static void LogToConsole(string message) =>
    Console.WriteLine(message);
```
The *New* method for `MyChores` requires an `Action<string>` to be passed.  It assigns this to itself and the various classes it uses.

```c#
public Chores(PhoneMessengerService messenger, Action<string> uiLogger)
{
    //.....
    this.UIMessenger = uiLogger;
    //.......
}
```
We define and initialize an instance of `MyChores` and other classes in `Main`.

```c#
var phonemessengerService = new PhoneMessengerService(LogToConsole);
//.....
var me = new UnemployedProgrammer(phonemessengerService, LogToConsole);
var chores = new MyChores(me, phonemessengerService, LogToConsole);
```

### Program.cs

Our first challenge in the console application is the switch from sync to async.

Make sure you are running the correct framework and latest language version. (C# 7.1 onwards supports a Task based `Main`).

```xml
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net5</TargetFramework>
    <LangVersion>latest</LangVersion>
    <RootNamespace>Async_Demo</RootNamespace>
  </PropertyGroup>
```
Pre #7.1, `Main` could only run synchronously, and you needed to do a "NONO", use a `Wait` to prevent `Main` dropping out the bottom of `Main` and closing the program. Post #7.1, declare `Main` returning a Task and `async` to declare `await`.

The `async` version looks like:

```c#
static async Task Main(string[] args)
{
    LogThreadType();
    var phonemessengerService = new PhoneMessengerService(LogToConsole);
    var phonemessengerServiceTask = Task.Run(() => phonemessengerService.Run());
    var me = new UnemployedProgrammer(phonemessengerService, LogToConsole);
    var chores = new JobScheduler(me, LogToConsole);
    var chorestask = chores.Start(me);
    await chorestask;
}
```
The code contains both versions of `Main`.

Change over to the sync version and comment out the `await`.

```c#
static void Main(string[] args)
{
    LogThreadType();
    var phonemessengerService = new PhoneMessengerService(LogToConsole);
    var phonemessengerServiceTask = Task.Run(() => phonemessengerService.Run());
    var me = new UnemployedProgrammer(phonemessengerService, LogToConsole);
    var chores = new JobScheduler(me, LogToConsole);
    var chorestask = chores.Start(me);
    // var chorestask = Task.Run(() => chores.Start(me)); 
    // chorestask.Wait();
}
```

And run it.

```text
[Main Thread][Program] running on Application Thread
[Main Thread][Messenger Service] > Messenger Service Created
[Messenger Service Thread][Messenger Service] >  running on Threadpool Thread
[Messenger Service Thread][Messenger Service] > Messenger Service Running
[Messenger Service Thread][Messenger] >  running on Threadpool Thread
[Main Thread][Chores Task Master] >  running on Application Thread
............
[Main Thread][Clothes Washing] > Can't continue till we have some powder!
[Main Thread][Application] > I'm multitasking > On task

Press any key to close this window . . .
```

`Main` drops out of the bottom once `chores.Start` yields back to `Main`, and closes, leaving `[Messenger Service Thread]` hanging.  Once it starts `chorestask` there's no next steps, so the program shuts down leaving the DotNetCore garbage collector cleans up the mess.

Change the Task running over to `Task.Run`

```c#
static void Main(string[] args)
{
    LogThreadType();
    var phonemessengerService = new PhoneMessengerService(LogToConsole);
    var phonemessengerServiceTask = Task.Run(() => phonemessengerService.Run());
    var me = new UnemployedProgrammer(phonemessengerService, LogToConsole);
    var chores = new JobScheduler(me, LogToConsole);
    //var chorestask = chores.Start(me);
    var chorestask = Task.Run(() => chores.Start(me)); 
    // chorestask.Wait();
}
```
It falls out even quicker.  Once the main thread has passed `chores.Start` over to the threadpool it's run out of things to do and exits.  

```text
[Main Thread][Program] running on Application Thread
[Main Thread][Messenger Service] > Messenger Service Created
[Messenger Service Thread][Messenger Service] >  running on Threadpool Thread
[Messenger Service Thread][Messenger Service] > Messenger Service Running
[Messenger Service Thread][Messenger] >  running on Threadpool Thread

Press any key to close this window . . .
```

Now add  `chorestask.Wait()` back in.

```c#
static async Task Main(string[] args)
{
    LogThreadType();
    var phonemessengerService = new PhoneMessengerService(LogToConsole);
    var phonemessengerServiceTask = Task.Run(() => phonemessengerService.Run());
    var me = new UnemployedProgrammer(phonemessengerService, LogToConsole);
    var chores = new JobScheduler(me, LogToConsole);
    // var chorestask = chores.Start(me);
    var chorestask = Task.Run(() => chores.Start(me)); 
    chorestask.Wait();
}
```

```
[Main Thread][Program] running on Application Thread
[Main Thread][Messenger Service] > Messenger Service Created
[Messenger Service Thread][Messenger Service] >  running on Threadpool Thread
[Messenger Service Thread][Messenger Service] > Messenger Service Running
[Messenger Service Thread][Messenger] >  running on Threadpool Thread
[Chores Task Master Thread][Chores Task Master] >  running on Threadpool Thread
[Chores Task Master Thread][Application] > I'm multitasking > On task
[Chores Task Master Thread][Good Morning] > Morning - what's on the agenda today!
......
Daydream of a proper job!

Press any key to close this window . . .
```

The program runs to completion.

Note:
1. `Main` starts on the `Main Thread`.
2. The Messenger service starts on it's own thread `Messenger Service Thread`.
3. `MyChores` runs on it's own `Chores Task Master Thread`. starting `MyChores` using  `Task.Run(() => chores.Start(me))` has switched it to a threadpool thread.

Now change the way the Task is run.  Change back to `var chorestask = chores.Start(me)`

```c#
static void Main(string[] args)
{
    LogThreadType();
    var phonemessengerService = new PhoneMessengerService(LogToConsole);
    var phonemessengerServiceTask = Task.Run(() => phonemessengerService.Run());
    var me = new UnemployedProgrammer(phonemessengerService, LogToConsole);
    var chores = new JobScheduler(me, LogToConsole);
    var chorestask = chores.Start(me);
    // var chorestask = Task.Run(() => chores.Start(me)); 
    chorestask.Wait();
}
```

Note the differences:
1. `Main` starts on the `Main Thread`.
2. The Messenger service starts on it's own thread `Messenger Service Thread`.
3. `MyChores` runs on the `Main Thread`.  Starting it by simply calling the method and assigning it to a variable `var chorestask = chores.Start(me)` starts it on the main application thread.  

```text
[Main Thread][Program] running on Application Thread
[Main Thread][Messenger Service] > Messenger Service Created
[Messenger Service Thread][Messenger Service] >  running on Threadpool Thread
[Messenger Service Thread][Messenger Service] > Messenger Service Running
[Messenger Service Thread][Messenger] >  running on Threadpool Thread
[Main Thread][Chores Task Master] >  running on Application Thread
[Main Thread][Application] > I'm multitasking > On task
[Main Thread][Good Morning] > Morning - what's on the agenda today!
[Main Thread][Good Morning] > Breakfast first
..........
[Main Thread][Clothes Washing] > Can't continue till we have some powder!
[Main Thread][Application] > I'm multitasking > On task
[Messenger Service Thread][Messenger Service] >  running on Threadpool Thread
[Messenger Service Thread][My Phone] > Ping! New message at 14:07:11. You have 1 unread messages.
........CHANGEOVER TO NEW THREAD HERE...............
[Chores Task Master Thread][Chores Task Master] > Long Running Tasks ==> Completed in (4004 millisecs)
[Chores Task Master Thread][Chores Task Master] > Long Running Tasks ==> Completed in (988 millisecs)
[Chores Task Master Thread][Breakfast] >  ???? No Wine in fridge?
..........
Daydream of a proper job!

Press any key to close this window . . .
```

Why did this run to completion?  Surely, `var chorestask = chores.Start(me))` starts the task on the same thread (no thread switch this time), assigns it to `chorestask`, and then when `chorestask` first yields,  `choresTask.Wait()` blocks the thread.  Under normal circumstances this would block, but, on the first yield of `chorestask` to `Main` and the call to `choresTask.Wait()`, the TaskScheduler running on the main application thread makes a decision to switch execution of `chorestask` to a new thread.  You can see this happening in the output shown above, and see when the first yield occurs by putting a breakpoint on `chorestask.Wait()` .

So our final `Main` looks like this:

```c#
static void Main(string[] args)
{
    LogThreadType();
    var phonemessengerService = new PhoneMessengerService(LogToConsole);
    var phonemessengerServiceTask = Task.Run(() => phonemessengerService.Run());
    var me = new UnemployedProgrammer(phonemessengerService, LogToConsole);
    var chores = new JobScheduler(me, LogToConsole);
    var chorestask = chores.Start(me);
    await chorestask;
}
```

### The PhoneMessengerService

Like everyone else in modernity, our programmer has a phone and gets messages.  `PhoneMessengerService` simulates a constant feed of drivel.

```c#
public class PhoneMessengerService : UILogger
{
    public bool Live
    {
        get => _Live;
        set
        {
            if (value && !_Live)
                this.MessengerTask = MessengeGenerator();
            _Live = value;
        }
    }

    public event EventHandler PingMessage;

    private bool _Live = true;

    protected Task MessengerTask { get; set; }

    public PhoneMessengerService(Action<string> uiLogger)
    {
        this.UIMessenger = uiLogger;
        this.CallerName = "Messenger Service";
        this.LogThreadType();
        this.MessengerTask = MessengeGenerator();
    }

    private async Task MessengeGenerator()
    {
        this.LogThreadType("Messenger");

        do
        {
            await Task.Delay(3000);
            PingMessage?.Invoke($"Hey, stuff going on at {DateTime.Now.ToLongTimeString()}!", EventArgs.Empty);
        } while (_Live);
    }
}
```

Note:
1. New forces you to give an instance an `Action<string>` to log application messages to.
2. New starts the `MessageGenerator` which generates a message every 3 seconds and passes it to anyone registered with the `PingMessage` event.
3. Live controls the service.  New sets up the genrator to run.
4. We use `Task.Delay` to control time.

`Main` starts the `PhoneMessengerService` and passes a reference to it to `chores`.

```c#
var phonemessengerService = new PhoneMessengerService(LogToConsole);
var chores = new Chores(phonemessengerService, LogToConsole);

```

### The UnemployedProgrammer

This class (rather simplistically) simulates a person doing jobs.  It's a state machine with three states: Idle, Multitasking and Busy.  Once awake in the morning they are either busy - single tasking on something  - or multitasking - standing on front of the washing machine pushing buttons or doodling around the shops.

Each Job controls the state. For example, calling `ImBusy` on an `UnemployedProgrammer` instance sets their state to busy.

The code is separated into three *Regions* to keep associated Properties, variables, events amd methods together.

Class initialisation looks like this:
```c#
public UnemployedProgrammer(PhoneMessengerService messenger, Action<string> uiLogger)
{
    // assign the PhoneMessengerService
    this.MessengerService = messenger;
    // assign the UIMessenger
    this.UIMessenger = uiLogger;
    // register with the PhoneMessengerService for the drivel feed
    MessengerService.PingMessage += OnMessageReceived;
}
```

#### Messaging

The full code block looks like this:

```c#
// assigned at class initialisation
private PhoneMessengerService MessengerService;

// tracks if there are messages waiting - which means we're already waiting for the DoNotDisturbTask to finish
private bool MessagesAlreadyWaiting = false;

// list of messages queued for reading
private List<string> MyMessages = new List<string>();

// event handler for incoming drivel
public void OnMessageReceived(object sender, EventArgs e)
{
    MyMessages.Add((string)sender);
    this.LogToUI($"Ping! New message at {DateTime.Now.ToLongTimeString()}. You have {MyMessages.Count} unread messages.", "My Phone");
    NotifyHaveMessages();
}

// Method to notify us there's a new message
public async void NotifyHaveMessages()
{
    if (!MessagesAlreadyWaiting)
    {
        MessagesAlreadyWaiting = true;
        var taskname = "Messages";
        await DoNotDisturbTask;
        this.LogToUI($"Reading Messages");
        var messages = new List<string>();
        messages.AddRange(MyMessages);
        MyMessages.Clear();
        foreach (var message in messages)
        {
            this.WriteDirectToUI($"{taskname} ==>> Message: {message}");
        }
        MessagesAlreadyWaiting = false;
    }
}
```

`OnMessageReceived` adds the message to `MyMessages` and calls `NotifyHaveMessages`.  If our programmer's not busy they read the message, if they're busy `NotifyHaveMessages` awaits the `DoNotDisturbTask` which completes when `State` is changed from Busy.  Note that we're using a Task defined as a property, not one returned by a Method.  We'll look at how the `DoNotDisturbTask` works later.  When `MessagesAlreadyWaiting` is `true` `NotifyHaveMessages` is already waiting and will deliver all the messages stacked in `MyMessages`.

#### State Management

State is controlled by the `State` property.  `Set` checks if we are actually changing to a new state, and if so sets the new state and triggers `StateChanged`

```c#
private StateType _State = StateType.Idle;

public StateType State
{
    get => this._State;
    protected set
    {
        if (value != this._State)
        {
            var oldstate = this._State;
            this._State = value;
            this.StateChanged(oldstate, value);
        }
    }
}

```
> A quick introduce to `TaskCompletionSource` - a class that provides functionality to manually manage a Task.  It's `Task` property exposes a standard out-of-the-box Task.  On class initialisation, `Task` is set to *WaitingForActivation* - i.e. a running Task as far as `await` is concerned.  There's various methods to transition it's state.  The most common is `SetResult()` which sets it to *RanToCompletion* -i.e. completed.

`StateChanged` checks:
1. If we're transitioning to *Busy*, in which case it creates a new  `DNDTasksCompletionSource`, and thus any assignment of `DoNotDisturbTask` references a running task. 
2. If we're transitioning from *Busy*, in which case we set `DNDTasksCompletionSource` to completed by calling `SetResult()`, and thus an *waits* on `DoNotDisturbTask` to complete. 

```c#
private void StateChanged(StateType oldstate, StateType newstate)
{
    if (newstate == StateType.Busy && DNDTasksCompletionSource.Task.IsCompleted)
        DNDTasksCompletionSource = new TaskCompletionSource();
    else if (oldstate == StateType.Busy && newstate != StateType.Busy)
        DNDTasksCompletionSource.SetResult();
}

private TaskCompletionSource DNDTasksCompletionSource = new TaskCompletionSource();

public Task DoNotDisturbTask
{
    get
    {
        if (this.IsBusy && DNDTasksCompletionSource.Task.IsCompleted)
            DNDTasksCompletionSource = new TaskCompletionSource<bool>();
        return DNDTasksCompletionSource.Task;
    }
}
```

The rest of the code provides methods to set and check `State`.

```c#
public bool IsBusy => this.State == StateType.Busy;
public bool IsMultitasking => this.State == StateType.Multitasking;
public bool IsIdle => this.State == StateType.Idle;

public void ImBusy(string message = null)
{
    message = message ?? Message;
    if (!string.IsNullOrWhiteSpace(message)) this.LogToUI($" I'm Busy > {message}");
    this.State = StateType.Busy;
}

public void ImIdle(string message = null)
{
    message = message ?? Message;
    if (!string.IsNullOrWhiteSpace(message)) this.LogToUI($"I'm Idle > {message}");
    this.State = StateType.Idle;
}

public void ImMultitasking(string message = null)
{
    message = message ?? Message;
    if (!string.IsNullOrWhiteSpace(message)) this.LogToUI($"I'm multitasking > {message}");
    this.State = StateType.Multitasking;
}
```
#### Jobs Management

The final section is the code to run and manage jobs.

We use two `enums` which are declared in the namespace.

```c#
    public enum PriorityType { Normal, Priority }
    public enum StateType { Idle, Multitasking, Busy }
```

A job is defined in the namespace as a simple `struct`.  Note `Job` is a function delegate with aa `UnemployedProgrammer` argument and a `Task` return value.

```c#
    public struct JobItem
    {
        public PriorityType Priority { get; set; }
        public Func<UnemployedProgrammer, Task> Job { get; set; }
    }
```
Two `public` methods let us queue jobs into the `UnemployedProgrammer`'s private `JobQueue`.  `QueueJob` lets us control when the jobs are loaded and run.

```c#
        private List<JobItem> JobQueue { get; } = new List<JobItem>();

        public void QueueJob(JobItem job, bool startnow = true)
        {
            this.JobQueue.Add(job);
            if (!this.JobsRunning && startnow)
                LoadAndRunJobs();
        }

        public void QueueJobs(List<JobItem> jobs)
        {
            foreach (var job in jobs)
                this.JobQueue.Add(job);
            LoadAndRunJobs();
        }
```
There are two `Task` queues into which jobs are loaded - `PriorityJobs` and `NormalJobs`.  There are two  `TaskCompletionSource` for tracking the state of the two queues, with two publically exposed `Task` properties.

```c#
public Task PriorityTasks =>
    PriorityTasksCompletionSource != null ?
    PriorityTasksCompletionSource.Task :
    Task.CompletedTask;

public Task AllTasks =>
    AllTasksCompletionSource != null ?
    AllTasksCompletionSource.Task :
    Task.CompletedTask;

private List<Task> NormalJobs { get; } = new List<Task>();

private List<Task> PriorityJobs { get; } = new List<Task>();

private TaskCompletionSource<bool> PriorityTasksCompletionSource { get; set; } = new TaskCompletionSource<bool>();

private TaskCompletionSource<bool> AllTasksCompletionSource { get; set; } = new TaskCompletionSource<bool>();
```
`LoadAndRunJobs` invokes the function delegate `Job` for each `JobItem` in `JobQueue` and places the returned `Task` into the appropriate queue.  It also sets up the `TaskCompletionSource` for each queue, and on completion clears `JobQueue`.

Once run, all the jobs are either running or scheduled with the `TaskScheduler`, and we have `Task` handles for each job in the appropriate queue.

```c#
private void LoadAndRunJobs()
{
    ClearAllTaskLists();
    if (JobQueue.Count > 0)
    {
        foreach (var job in JobQueue)
        {
            if (AllTasksCompletionSource.Task.IsCompleted)
                AllTasksCompletionSource = new TaskCompletionSource<bool>();
            if (job.Priority == PriorityType.MustCompleteNow)
            {
                if (PriorityTasksCompletionSource.Task.IsCompleted)
                    PriorityTasksCompletionSource = new TaskCompletionSource<bool>();
                var task = job.Job.Invoke(this);
                this.PriorityJobs.Add(task);
            }
            else
            {
                var task = job.Job.Invoke(this);
                this.NormalJobs.Add(task);
            }
        }
        JobQueue.Clear();
    }
}
```
The final bit for job monitoring is to provide external methods to get notifications when the job queues have completed.  We provide these through two `Tasks` - `PriorityJobsMonitorAsync` and `AllJobsMonitorAsync`.

> `WhenAll` is one of several static `Task` methods.  `WhenAll` creates a single `Task` which `awaits` all the Tasks in the submitted array.  It's status will change to *Complete* when all the Tasks complete.  `WhenAny` is similar, but will be set to *Complete* when any not all are complete.  They could be named *AwaitAll* and *AwaitAny*.  `WaitAll` and `WaitAny` are block versions equating with `Wait`.  Not sure about the reasons for the slightly confusing naming conversion - I'm sure there was one.

`PriorityJobsMonitorAsync` demonstrates how these work.  It calls `ClearPriorityTaskList` which clears all completed `Tasks` from the `PriorityJobs` queue.  If there are any jobs in the queue, it creates an returns a `Task.WhenAll` for all the remaining running tasks.  `AllJobsMonitorAsync` does the same thing for both queues.

```c#

        public Task PriorityJobsMonitorAsync()
        {
            ClearPriorityTaskList();
            if (PriorityJobs.Count > 0)
                return Task.WhenAll(PriorityJobs.ToArray());
            else
                return Task.CompletedTask;
        }

        public Task AllJobsMonitorAsync()
        {
            ClearAllTaskLists();
            var jobs = new List<Task>();
            {
                jobs.AddRange(this.PriorityJobs);
                jobs.AddRange(this.NormalJobs);
            }
            if (jobs.Count > 0)
                return Task.WhenAll(jobs.ToArray());
            else
                return Task.CompletedTask;
        }

        public void ClearAllTaskLists()
        {
            this.ClearPriorityTaskList();
            var removelist = this.NormalJobs.Where(item => item.IsCompleted).ToList();
            removelist.ForEach(item => this.NormalJobs.Remove(item));
            if (this.NormalJobs.Count == 0)
                this.AllTasksCompletionSource.SetResult(true);
        }

        public void ClearPriorityTaskList()
        {
            var removelist = this.PriorityJobs.Where(item => item.IsCompleted).ToList();
            removelist.ForEach(item => this.PriorityJobs.Remove(item));
            if (this.PriorityJobs.Count == 0)
                this.PriorityTasksCompletionSource.SetResult(true);
        }

```
### ShoppingJob

`ShoppingJob` is a class that represents a planned trip to the shops.  It holds the shopping list and implements `IEnumerable`.

> `IEnumerable` is the easiest enumerator interface to declare on a class.  You don't need to implement all the complexity of `ICollection`.  

The properties and private fields are:
```c#
// Bool - True if there are items in the shopping list
public bool NeedToShop => List.Count > 0;
// TaskCompletionSource for controlling the exposed property ShoppingTask 
TaskCompletionSource<bool> ShoppingTaskCompletionSource = new TaskCompletionSource<bool>();
// Shopping list
private List<string> List = new List<string>();
// tracker of the number of times we've been to the shops!
public int TripsToShop { get; private set; } = 0;
```

`Add` adds a new item to the shopping list - and writes a message to the UI - and if the shopping list was empty. creates a new instance of `ShoppingTaskCompletionSource`.

```c#
public void Add(string item)
{
    if (ShoppingTaskCompletionSource is null | ShoppingTaskCompletionSource.Task.IsCompleted)
    {
        this.WriteDirectToUI("  +++> Starting New Shopping List");
        ShoppingTaskCompletionSource = new TaskCompletionSource<bool>();
    }
    List.Add(item);
    this.WriteDirectToUI($"  ===> {item} added to Shopping List");
}

```
`ShoppingTask` exposes a `Task` that can be used to track when the shopping has been done.  Any jobs can await the `ShoppingJob` until the shopping it's put on the list is done.  The `WashingUpChore` does this for washing Up Liquid.

```c#
public Task ShoppingTask
{
    get
    {
        if (NeedToShop && ShoppingTaskCompletionSource is null)
        {
            ShoppingTaskCompletionSource = new TaskCompletionSource<bool>();
            return ShoppingTaskCompletionSource.Task;
        }
        else if (NeedToShop)
            return ShoppingTaskCompletionSource.Task;
        else
            return Task.CompletedTask;
    }
}
```
The shopping is done, and the list cleared when the shopper calls `ShoppingDone`

```c#
public void ShoppingDone()
{
    List.Clear();
    TripsToShop++;
    this.WriteDirectToUI($"  ===> Cleared Shopping List");
    ShoppingTaskCompletionSource.SetResult(true);
}
```

The final bit of `ShoppingTask` is implementing `IEnumerable`.

The helper class looks like this:

```c#
public class ShoppingJobEnumerator : IEnumerator
{
    private List<string> _items = new List<string>();
    private int _cursor;

    object IEnumerator.Current
    {
        get
        {
            if ((_cursor < 0) || (_cursor == _items.Count))
                throw new InvalidOperationException();
            return _items[_cursor];
        }
    }
    public ShoppingJobEnumerator(List<string> items)
    {
        this._items = items;
        _cursor = -1;
    }
    void IEnumerator.Reset() =>
        _cursor = -1;

    bool IEnumerator.MoveNext()
    {
        if (_cursor < _items.Count)
            _cursor++;

        return (!(_cursor == _items.Count));
    }
}
```

And this is used in the class as follows:

```c#
public IEnumerator GetEnumerator() =>
    new ShoppingJobEnumerator(List);
```



## Job Scheduler

`JobScheduler` is the hub for running jobs.

Initialisation maps the UILogger and assigns the default person.

It has three self-evident properties:

```c#
StockList OurStocks = new StockList()
{
    HooverBags = 1,
    WashingUpLiquid = 0,
    SoapPowder = 0
};

ShoppingJob ShoppingList { get; } = new ShoppingJob();

private UnemployedProgrammer Me {get; set;}
```

`Start` loads today's jobs onto our Unemployed Programmer, returning a `Task` for tracking.  It shows the two ways of loading a `JobItem`.

```c#
        public async Task Start(UnemployedProgrammer me = null)
        {
            var taskname = "Run Jobs";
            this.LogThreadType();
            this.Me = me ?? this.Me;
            var taskstorun = new List<JobItem>();
            {
                taskstorun.Add(new JobItem() { Job = GoodMorning, , Priority = PriorityType.Priority });
                taskstorun.Add(new JobItem() { Job = HaveBreakfast, Priority = PriorityType.Priority });
                taskstorun.Add(new JobItem() { Job = WashingUpChore, Priority = PriorityType.Priority });
                taskstorun.Add(new JobItem() { Job = TheWashingChore, Priority = PriorityType.Normal });
                taskstorun.Add(new JobItem() { Job = GoToTheShops, Priority = PriorityType.Priority });
            }
            this.Me.QueueJobs(taskstorun);
            await me.PriorityJobsMonitorAsync();
            this.Me.QueueJob(new JobItem() { Job = HooveringChore, Priority = PriorityType.Priority }, false);
            this.Me.QueueJob(new JobItem() { Job = GoToTheShops, Priority = PriorityType.Priority });
            await me.AllJobsMonitorAsync();
            this.LogToUI("All done, feet up.", taskname);
            this.WriteDirectToUI("Daydream of a proper job!");
        }
```

  Lets break this down to look in detail and what's going on:
1. We build a `List<JobItem>` and then call `QueueJobs` and pass it the list.
2. `Me.QueueJobs` queues the jobs into `Me.JobQueue`.
3. `Me.QueueJobs` calls `Me.LoadAndRunJobs`.
4. `Me.LoadAndRunJobs`.
   1. Iterates through `JobQueue`.
   2. Starts each job though `Invoke`, passing the invoked `Func` a reference to `Me`.
   3. Puts the returned `Task into the appropriate` Task Queue.
   4. Clears `Me.JobQueue`.
   5. Returns control to `Me.QueueJobs`.
5.  Returns control to `Start`.
   
At this point the five jobs are with the TaskScheduler and running.

Let's look at a couple of them:

Breakfast is just a sequence of events with a *LongRunTask* to simulate sitting down and eating breakfast.  We haven't set busy, so we can answer our phone if it pings.
```c#
public async Task HaveBreakfast()
{
    var taskname = "Breakfast";
    this.LogToUI($"In the fridge", taskname);
    this.LogToUI($"No eggs, so it's toast only.", taskname);
    ShoppingList.Add("Eggs");
    this.LogToUI($"Last two pieces of bread used up", taskname);
    ShoppingList.Add("Bread");
    await LongRunTask.RunAsync(5, $"Eating");
    this.LogToUI($" ???? No Wine in fridge?", taskname);
    ShoppingList.Add("Wine");
}
```
```
[Main Thread][Morning Chores] > Morning - what's on the agenda today!
[Main Thread][Morning Chores] > Breakfast first
[Main Thread][Breakfast] > In the fridge
[Main Thread][Breakfast] > No eggs, so it's toast only.
  ===> Eggs added to Shopping List
[Main Thread][Breakfast] > Last two pieces of bread used up
  ===> Bread added to Shopping List
[My Phone Thread][My Phone] > Ping! New message at 17:06:24. You have 1 unread messages.
[My Phone Thread][Chores Task Master] > Reading Messages
Messages ==>> Message: Hey, stuff going on at 17:06:24!
[My Phone Thread][Chores Task Master] > Phone back in pocket.
[Main Thread][Activity Task] > Eating ==> Completed in (3965 millisecs)
[Main Thread][Breakfast] >  ???? No Wine in fridge?
```
Check the threads in the above sequence.  Phone messages are coming in on `[My Phone Thread]`.  If you check the initilisation sequence higher you will find that `PhoneMessengerService` started on the `Main Thread`.  When the service code hit the first `await Task.Delay(3000)`, the TaskScheduler on the thread made an internal decision to switch the task to a new thread.

```
[Main Thread][Messenger Service] >  running on Application Thread
[Main Thread][Messenger] >  running on Application Thread
```

If you look at the full sequence you'll see the same thing happening with `TheWashingChore`.  After the `ShoppingTask` completes there's `await Task.Delay(7000)` which again gives the TaskScheduler a chance to make a scheduling decision.

```
[Main Thread][Clothes Washing] > Loading the washing machine and
[Main Thread][Clothes Washing] > Check if we have powder
[Main Thread][Clothes Washing] > No soap powder!
  ===> Soap Powder added to Shopping List
[Main Thread][Clothes Washing] > Can't continue till we have some powder!

... go to the shops .....

[Main Thread][Shopping] > Back Home. Shopping done.
  ===> Cleared Shopping List
[Clothes Washing Thread][Clothes Washing] > Add the powder, Click the button and stand back
[Clothes Washing Thread][Clothes Washing] > washing...
[Main Thread][Busy Task] > Busy Doing the Washing Up
```
Let's now look at an actual "task" in detail - the `WashingUpChore`.

It:
1. Checks to see if there's washinbg up liquid.
2. If not it adds it to the shopping list and then awaits it's arrival (after shopping)
3. It then waits on any Tasks in the `awaitlist`.  More detail below.
4. Finally it attempts to run the `task` defined higher up in the code synchronously.
5. This sets `ImBusy` to busy, and `awaits` a *longrunningtask* to simulate hands in the sink.
6. It finally sets `ImBusy` to idle and completes.

```c#
public async Task WashingUpChore(Task[] awaitlist)
{
    var taskname = $"Washing Up";
    var threadname = Thread.CurrentThread.Name;
    var completiontask = new Task(() =>
    {
        ImBusy.SetBusy("Doing the Washing Up");
        LongRunTask.Run(10, $"Washing up");
        ImBusy.SetIdle();
        this.LogToUI($"Washing Up Done", taskname);
    });

    this.LogToUI($"Check if I can do the washing up (any excuse will do!)",taskname);
    if (this.OurStocks.WashingUpLiquid < 1)
    {
        this.LogToUI($"mmmm - can't find any Washing Up Liquid", taskname);
        ShoppingList.Add("Washing Up Liquid");
        this.LogToUI($"Can't continue till we have some washing Up Liquid!", taskname);
        await ShoppingList.ShoppingTask;
    }
    Task.WaitAll(awaitlist);
    this.LogToUI($"Back to the sink", taskname);
    completiontask.RunSynchronously();
}
```

This code runs as expected.  Our programmer is busy.  The phone pings, but there's no checking the messages or doing anything else.

If you now change the `completiontask` code to:

```c#
var completiontask = new Task(() =>
{
    LongRunTask.Run(10, $"Washing up");
    this.LogToUI($"Washing Up Done", taskname);
});
```

The result may be little suprising.  We've set the code block to run synchronously with `completiontask.RunSynchronously()` and the code within `completiontask` is synchronous - no waits or delays, just a processor intensive calculation.  So why are we checking messages with our rubbers on and messing with the washing machine? 

```text
[Main Thread][Washing Up] > Back to the sink
[Clothes Washing Thread][Clothes Washing] > Add the powder, Click the button and stand back
[Clothes Washing Thread][Clothes Washing] > washing...
[My Phone Thread][My Phone] > Ping! New message at 18:07:39. You have 1 unread messages.
[My Phone Thread][Chores Task Master] > Reading Messages
Messages ==>> Message: Hey, stuff going on at 18:07:39!
[My Phone Thread][Chores Task Master] > Phone back in pocket.
[My Phone Thread][My Phone] > Ping! New message at 18:07:42. You have 1 unread messages.
[My Phone Thread][Chores Task Master] > Reading Messages
Messages ==>> Message: Hey, stuff going on at 18:07:42!
[My Phone Thread][Chores Task Master] > Phone back in pocket.
[Clothes Washing Thread][Clothes Washing] > PING!! PING!!! Washing complete!
[My Phone Thread][My Phone] > Ping! New message at 18:07:45. You have 1 unread messages.
[My Phone Thread][Chores Task Master] > Reading Messages
Messages ==>> Message: Hey, stuff going on at 18:07:45!
[My Phone Thread][Chores Task Master] > Phone back in pocket.
[Main Thread][Activity Task] > Washing up ==> Completed in (8817 millisecs)
[Main Thread][Washing Up] > Washing Up Done
```

The clue is in the threads that each process is running on.  `Main Thread` is blocked by the *longRunningTask*, but `Clothes Washing Thread` and `My Phone Thread` aren't.  We've lost our analogy with our programmer, we have our programmer and two alter egos on the job.  I've solved this problem with the Busy class to track our programmer's state.

