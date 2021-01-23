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
We define and initialize an instance of `NyChores` and other classes in `Main`.

```c#
var phonemessengerService = new PhoneMessengerService(LogToConsole);
//.....
var me = new UnemployedProgrammer(phonemessengerService, LogToConsole);
var chores = new MyChores(me, phonemessengerService, LogToConsole);
```

### Program.cs

Our first challenge in the console application is the switch from sync to async.

```c#
static async void Main(string[] args)
{
    //......
    await chores.Start(me);
}
```
Unfortunately, this won't compile.  You can't have an async `main` - not allowed.

There are two choices:
1. Build an async context - fairly complicated, you can find out about building one [here](https://blog.stephencleary.com/2013/04/implicit-async-context-asynclocal.html). 
2. Do a NONO - use `Wait`.
3. Run `MyChores` on the main thread.
4. 
Let's look at 2 and 3.

Our next pass at `Main` may look like this. If you have the project setup, modify `Main` and run it.

```c#
static void Main(string[] args)
{
    LogThreadType();
    var phonemessengerService = new PhoneMessengerService(LogToConsole);
    var phonemessengerServiceTask = Task.Run(() => phonemessengerService.Run());
    var me = new UnemployedProgrammer(phonemessengerService, LogToConsole);
    var chores = new MyChores(me, phonemessengerService, LogToConsole);
    var chorestask = Task.Run(() => chores.Start(me));
}
```
What you get is something like:

```c#
[Main Thread][Program] running on Application Thread
[Main Thread][Messenger Service] > Messenger Service Created
[Messenger Service Thread][Messenger Service] >  running on Threadpool Thread
[Messenger Service Thread][Messenger Service] > Messenger Service Running
[Messenger Service Thread][Messenger] >  running on Threadpool Thread

Press any key to close this window . . .
```

A failure, but a lesson.  `Task.Run(() => chores.Start(me))` switched `chores` to the Threadpool, while the main Application Thread ran to completion - there's no code after `Task.Run` - and closed the application.  The threadpool thread was left hanging, attached to nothing, and was cleared up by the DotNetCore garbage collector.

Add a `Wait` as shown below:

```c#
static void Main(string[] args)
{
    LogThreadType();
    var phonemessengerService = new PhoneMessengerService(LogToConsole);
    var phonemessengerServiceTask = Task.Run(() => phonemessengerService.Run());
    var me = new UnemployedProgrammer(phonemessengerService, LogToConsole);
    var chores = new MyChores(me, phonemessengerService, LogToConsole);
    var chorestask = Task.Run(() => chores.Start(me));
    chorestask.Wait();
}
```

```
[Main Thread][Program] running on Application Thread
[Main Thread][Messenger Service] >  running on Application Thread
[Main Thread][Messenger] >  running on Application Thread
[Chores Task Master Thread][Chores Task Master] >  running on Threadpool Thread
[Chores Task Master Thread][Morning Chores] > Morning - what's on the agenda today!
[Chores Task Master Thread][Morning Chores] > Breakfast first
..........
Daydream of a proper job!

Press any key to close this window . . .

```
The program now runs to completion.  Why no deadlock?  `chores.ChoresTask.Wait()` is a blocking action, but it's only blocking the Main Application thread.  Chores is running on *Chores Task Master Thread* a Threadpool thread.  `Task.Run(() => chores.Start())' switched the task to the threadpool.

Now let's change `Main` again.

```c#
static void Main(string[] args)
{
    LogThreadType();
    var phonemessengerService = new PhoneMessengerService(LogToConsole);
    var phonemessengerServiceTask = Task.Run(() => phonemessengerService.Run());
    var me = new UnemployedProgrammer(phonemessengerService, LogToConsole);
    var chores = new MyChores(me, phonemessengerService, LogToConsole);
    var chorestask = chores.Start(me);
    chorestask.Wait();
}
```

Again it runs to completion.  Why?  Surely, `var chorestask = chores.Start(me))` starts the task on the same thread (no thread switch this time), assigns it to `chorestask`, and then `chores.ChoresTask.Wait()` blocks the thread.  Under normal circumstances this would block, but here we're running as a console application.  `Program` runs in a non-async application context - the clue is in the fact that we can't declare `Main` as async .  It's synchronous, and runs sequentially.  `chores.Start()` loads it's code onto the application thread and runs to completion before `Main` moves to the next line of code, `chores.ChoresTask.Wait()`.  At which point there's nothing to wait on and block, `chorestask` has completed.  Put a break point on `chores.ChoresTask.Wait()` and see when it gets hit.  `chores.ChoresTask.Wait()` is redundant.

So our final `Main` looks like this:

```c#
static void Main(string[] args)
{
    LogThreadType();
    var phonemessengerService = new PhoneMessengerService(LogToConsole);
    var phonemessengerServiceTask = Task.Run(() => phonemessengerService.Run());
    var me = new UnemployedProgrammer(phonemessengerService, LogToConsole);
    var chores = new MyChores(me, phonemessengerService, LogToConsole);
    var chorestask = chores.Start(me);
}
```
So, in reality both 2 and 3 are valid ways to run the program.  Take your choice.

### The UnemployedProgrammer

This class (rather simplistically) simulates a person doing jobs.  It's a state machine with three states: Idle, Multitasking and Busy.  Once awake in the morning they are either busy - single tasking on something  - or multitasking - standing on front of the washing machine pushing buttons or doodling around the shops.

Each Job controls the state. For example, calling `ImBusy` on an `UnemployedProgrammer` instance sets their state to busy.



The Busy task keeps track of our programmer's state, they're busy doing something, or idling waiting for something to happen.  When they start a task that needs their full attention they set `ImBusy` by calling `SetBusy` and once complete call `SetIdle`.  You can't answer your phone in rubber gloves at the sink!

`Busy` demonstrates a key skill, how to create and control a task.  It exposes a Task as `Busy.Task` which consumer classes can use to check on our programmer's state.

Internally we use the `TaskCompletionSource` class to create a manual task processor instance `taskCompletionSource` when req.  When the `Busy` initialises it's in ible state - `Busy.Task` returns a completed Task.

When `SetBusy` is called, the method creates a new `TaskCompletionSource` instance, which initialises in the "processing" state.  Any call to `Busy.Task` will return `taskCompletionSource.Task`, a reference to the `Task` exposed by  `taskCompletionSource` instance.  This will be a task in the "Running" state.

When `SetIdle` is called `taskCompletionSource.SetResult(true)` sets the state to "complete" and the `Busy.Task` reports the Task as complete.  Any process that has a reference to `Busy.Task` completes, and any await stop waiting and moves on.

```c#
public class Busy :UILogger
{
    TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

    public Busy(bool isbusy)
    {
        taskCompletionSource = new TaskCompletionSource<bool>();
        if (!isbusy) taskCompletionSource.SetResult(true);
    }

    public void SetIdle()
    {
        var taskname = "Busy Task";
        taskCompletionSource.SetResult(true);
        this.LogToUI($"finished {Message}",taskname);
    }

    public Task Task =>
        taskCompletionSource != null ?
        taskCompletionSource.Task :
        Task.CompletedTask;

    private string Message = "Doing Things";

    public void SetBusy(string message = null)
    {
        Message = message ?? Message;

        var taskname = "Busy Task";
        taskCompletionSource = new TaskCompletionSource<bool>();
        this.LogToUI($"Busy {Message}", taskname);
    }
}
```
We can see `ImBusy` being set in the `WashingUpChore` in `Chores`.

```c#
ImBusy.SetBusy("Doing the Washing Up");
await LongRunTask.RunAsync(5, $"Washing up");
ImBusy.SetIdle();
```

And being monitored in `NotifyHaveMessages`

```c#
if (!MessagesAlreadyWaiting)
{
    MessagesAlreadyWaiting = true;
    var taskname = "Messages";
    await ImBusy.Task;
    /// read messages once free
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
1. New forces you to give it an `Action<string>` to log (application) messages to.
2. New starts up the `MessageGenerator` which generates a message every 3 seconds and passes it to anyone registered with the `PingMessage` event.
3. Live controls the service.  New sets up the genrator to run.
4. We use `Task.Delay` to control time.

`Main` starts the `PhoneMessengerService` and passes a reference to it to `chores`.

```c#
var phonemessengerService = new PhoneMessengerService(LogToConsole);
var chores = new Chores(phonemessengerService, LogToConsole);

```
`Chores` sets a local global to the passed `PhoneMessengerService` and registers `OnMessageReceived` with the `PingMessage` event

```c#
///....
private PhoneMessengerService Messenger;
private List<string> MyMessages = new List<string>();
///....
public Chores(PhoneMessengerService messenger, Action<string> uiLogger)
{
    ///...
    Messenger = messenger;
    Messenger.PingMessage += OnMessageReceived;
}

public void OnMessageReceived(object sender, EventArgs e)
{
    MyMessages.Add((string)sender);
    this.LogToUI($"Ping! New message at {DateTime.Now.ToLongTimeString()}. You have {MyMessages.Count} unread messages.", "My Phone") ;
    NotifyHaveMessages();
}
```
`OnMessageReceived` adds the message to `MyMessages` and calls `NotifyHaveMessages`.  If our programmer is idle they read the message, if thet're busy `NotifyHaveMessages` waits till they're idle.

```c#
public async void NotifyHaveMessages()
{
    if (!MessagesAlreadyWaiting)
    {
        MessagesAlreadyWaiting = true;
        var taskname = "Messages";
        await ImBusy.Task;
        this.LogToUI($"Reading Messages");
        var messages = new List<string>();
        messages.AddRange(MyMessages);
        MyMessages.Clear();
        foreach (var message in messages)
        {
            this.WriteDirectToUI($"{taskname} ==>> Message: {message}");
        }
        MessagesAlreadyWaiting = false;
        this.LogToUI($"Phone back in pocket.");
    }
}
```
## Chores

`Chores` is the hub for running today's tasks.

New sets up the object:
1. Sets up the Name for logging
2. Maps the logger Action to sub classes.
3. Sets up the new Logruntask for simulating work.
4. Registers `OnMessageReceived` for phone messages.  See the aboce Phone Messenger section

```c#
public Chores(PhoneMessengerService messenger, Action<string> uiLogger)
{
    this.CallerName = "Chores Task Master";

    this.UIMessenger = uiLogger;
    this.ShoppingList.UIMessenger = uiLogger;
    this.ImBusy.UIMessenger = uiLogger;

    LongRunTask = new PrimeTask(uiLogger);

    Messenger = messenger;

    Messenger.PingMessage += OnMessageReceived;
}
```

`Start` starts todays chores.  It logs the Thread Information and then sets the `Task` property to the `DoMyChores` and returns the same task.

```c#
public Task Start()
{
    this.LogThreadType();
    this.ChoresTask = DoMorningChores();
    return this.ChoresTask;
}
```
`DoMyChores` is the main Chore Manager setting out what takes place when and what waits on what.

```c#
public async Task DoMyChores()
{
    var taskname = "Morning Chores";

    this.LogToUI($"Morning - what's on the agenda today!", taskname);
    // Breakfast first, nothing except the phone disturbs that
    this.LogToUI("Breakfast first", taskname);
    await HaveBreakfast();
    // Start Washing up
    var doWashingUpChore = WashingUpChore(new Task[] { });
    // Get the washing machine going
    var doTheWashingChore = TheWashingChore(new Task[] { });
    // Go to the shops if we have items on the shopping list.  We're bored and need to get out!
    // await as we're out of the house and can't do anythine else.
    await GoToTheShops();
    // make sure the washing up is done
    await doWashingUpChore;
    // Start hoovering
    var doHoovering = HooveringChore(new Task[] { doWashingUpChore });
    // got to the shops if we have items we need to complete today's tasks
    await GoToTheShops();
    // make sure the Washing and Hoovering is done before we put our feet up
    Task.WaitAll(new Task[] { doTheWashingChore, doHoovering });
    // Done
    this.LogToUI("All done, feet up.", taskname);
    this.WriteDirectToUI("Daydream of a proper job!");
}
```
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

