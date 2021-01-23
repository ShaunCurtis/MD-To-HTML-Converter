# Building Workflows in DotNetCore and Blazor

This article describes how to build a workflow framework in DotNetCore and host the frend end on Blazor. 

The code is avalable at [Blazor.Workflow](https://github.com/ShaunCurtis/Blazor.Workflow) on GitHub.

## Introduction

Workflows have always interested me.  One of my planned retirement projects was developing a core workflow framework.  Four years in, and here's my first pass.


## Generics

Generics are used extensively in the project to build base classes containing most of the bolier plate code.

The generic "terms" used are:

**TRecord**

`TRecord` defines data records. For normal use they're restricted as:
```c#
where TRecord : class, IDbRecord<TRecord>, new()
```
or specifically in Workflow classes where we need some extra record functionality:
```c#
where TRecord : class, IWorkflowDbRecord<TRecord>, IDbRecord<TRecord>, new()
```

**TDbContext**

`TDbContext` defines database contexts.  They're restricted as:
```c#
where TDbContext : DbContext
```
**TWorkflowState**

`TWorkflowState` defines Workflow States.  They're restricted as:
```c#
where TWorkflowState : class, IWorkflowState
```
## Authentication and Authorization

All users using the application must be authenicated, and their access controlled by authorization.  We'll be using Azure to host the application so we'll implment authenication with Azure's B2C Active Directory.  I'm not going to cover setting this up in detail, there's plenty of information out there on doing this - see:

1. [Microsoft Azure How Tos](https://docs.microsoft.com/en-us/azure/active-directory-b2c/tutorial-create-tenant)
2. [Quick Guide on Medium.com](https://medium.com/marcus-tee-anytime/azure-ad-b2c-quickstart-with-visual-studio-blazor-563efdff6fdd)

You will need an Azure account.  You can sign up for a free account [here](https://azure.microsoft.com/en-gb/free/).

> Need to fix this
 
The basic process is:
1. Add an Azure AD B2C tenant.
2. Switch to the new directory. You'll need the **Resource Name** - this application is: CECWorkflows.onmicrosoft.com.
3. Add a Application (your web site).  You'll need the **Application (client) ID** - it's a Guid.
4. Add a User Flow - Sign up and sign in - for the login in process. my is called **B2C_1_NormalUsers**
5. Switch back to your default Tenancy and select Azure AD B2C.
6. Add an Application Registration - this will be for Micrsosoft Accounts.

## Application

Add a section to you site *appsetting.json*

```c#
  "AzureAdB2C": {
    "Instance": "https://CECWorkflows.b2clogin.com/tfp/",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "CallbackPath": "/signin-oidc",
    "Domain": "CECWorkflows.onmicrosoft.com",
    "SignedOutCallbackPath": "/signout/B2C_1_susi",
    "SignUpSignInPolicyId": "B2C_1_NormalUsers",
    "ResetPasswordPolicyId": "B2C_1_NormalUsers",
    "EditProfilePolicyId": "B2C_1_NormalUsers"
  },

```
Within the application we use `WorkflowPermissions` objects for controlling authorization. A `WorkflowPermissions` is a `ICollection` with exposed permission checker methods.

```c#
    public class WorkflowPermissions : ICollection
    {
        private List<WorkflowPermission> _items = new List<WorkflowPermission>() ;
        public int Count => _items.Count;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        public bool HasReadPermission(Guid guid)
        {
            var x = _items.FirstOrDefault(item => item.Guid.Equals(guid));
            return (x != null && x != default && x.Permissions.HasFlag( WorkflowPermissionType.Read));
        }

        public bool HasReadPermissions(List<Guid> guids)
        {
            foreach (var guid in guids)
            {
                if (HasReadPermission(guid)) return true;
            }
            return false;
        }
        // Lots of Permission Checker Methods and ICollection implementation 
```

`WorkflowPermission` looks like this:

```c#
    [Flags]public enum WorkflowPermissionType { None = 0, Read = 1, Write = 2, ReadWrite = Read | Write }
    public enum WorkflowPermissionType { None = 0, Group = 1, User = 2 }
   
    public class WorkflowPermission
    {
        public string Name { get; init; }
        public Guid Guid { get; init; }
        public WorkflowPermissionAccessType Permissions { get; init; } = WorkflowPermissionAccessType.None;
        public WorkflowPermissionType PermissionType { get; init; } = WorkflowPermissionType.None;

        // .... Class initialization methods
    }
```



## SQL Database

The database design is kept relatively simple - it should be easy to port.

Workflow table - tracks the start of all workflows.
```sql
CREATE TABLE [dbo].[Workflow](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[GUID] [uniqueidentifier] NOT NULL,
	[WorkflowName] [nvarchar](128) NULL,
	[WorkflowStateGUID] [uniqueidentifier] NOT NULL,
	[WorkflowData] [nvarchar](max) NULL,
	[WorkflowLog] [nvarchar](max) NULL,
 CONSTRAINT [PK_WorkflowInstance] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Workflow] ADD  CONSTRAINT [DF_WorkflowInstance_GUID]  DEFAULT (newid()) FOR [GUID]
GO
```
Visitors table - a dataset for a workflow:

```sql
CREATE TABLE [dbo].[Visitor](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[GUID] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
	[VisitDate] [smalldatetime] NOT NULL,
	[WorkflowGUID] [uniqueidentifier] NOT NULL,
	[Visiting] [nvarchar](128) NULL,
 CONSTRAINT [PK_Visitor] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Visitor] ADD  CONSTRAINT [DF_Visitor_GUID]  DEFAULT (newid()) FOR [GUID]
GO
```
Users table - basic user information pulled from the Authenticator.  Linked to the Authenticated user by the GUID.
```sql
CREATE TABLE [dbo].[User](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[GUID] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](255) NULL,
	[Groups] [nvarchar](max) NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
```

WorkflowState table - list of Workflow States that can be used in joins to the Workflow to display the workflow state without loading the full workflow.

```sql
CREATE TABLE [dbo].[WorkflowState](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[WorkFlowState] [varchar](50) NOT NULL,
	[GUID] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_WorkflowState] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[WorkflowState] ADD  CONSTRAINT [DF_WorkflowState_GUID]  DEFAULT (newid()) FOR [GUID]
GO
```

List, Read and Display operations use Views.

User View
```sql
CREATE VIEW [dbo].[vw_User]
AS
SELECT        ID, GUID, Name, Groups
FROM            dbo.[User]
GO
```

Visitor Workflow view - combining data from the Visitor and Workflow tables.

```sql
CREATE VIEW [dbo].[vw_VisitorWorkflow]
AS
SELECT        dbo.Visitor.ID, dbo.Visitor.GUID, dbo.Visitor.Name, dbo.Visitor.VisitDate, dbo.Workflow.ID AS WorkflowID, dbo.Visitor.WorkflowGUID, dbo.Workflow.WorkflowName, dbo.Workflow.WorkflowData, dbo.Workflow.WorkflowLog, 
                         dbo.Workflow.WorkflowStateGUID, ISNULL(dbo.WorkflowState.WorkFlowState, 'None') AS WorkflowState
FROM            dbo.Visitor LEFT OUTER JOIN
                         dbo.Workflow ON dbo.Visitor.WorkflowGUID = dbo.Workflow.GUID LEFT OUTER JOIN
                         dbo.WorkflowState ON dbo.Workflow.WorkflowStateGUID = dbo.WorkflowState.GUID
GO
```

Create and Update for Users.

```sql
CREATE PROCEDURE [dbo].[sp_Add_User]
@ID int output,
@Name varchar(128),
@GUID uniqueidentifier,
@Groups nvarchar(max)

AS
BEGIN
		INSERT 
			INTO [User] ([Name], [GUID], [Groups])
			VALUES (@Name, @GUID, @Groups)

		SELECT @ID = SCOPE_IDENTITY();
END
GO

CREATE PROCEDURE [dbo].[sp_Update_User]
@ID int = 0,
@Name varchar(128),
@GUID uniqueidentifier,
@Groups nvarchar(max)

AS
BEGIN
		UPDATE [User]
			SET Name = @Name, Groups = @Groups
    		WHERE ID = @ID
END
GO
```

Create and Update for Visitor Workflow.

```sql
CREATE PROCEDURE [dbo].[sp_Add_VisitorWorkflow]
@ID int output,
@GUID uniqueidentifier,
@Name varchar(128),
@VisitDate smalldatetime,
@WorkflowGUID uniqueidentifier,
@WorkflowName varchar(128),
@WorkflowStateGUID uniqueidentifier,
@WorkflowData varchar(max),
@WorkflowLog varchar(max)

AS
BEGIN
DECLARE @NewID int = 0;

BEGIN TRANSACTION
		INSERT 
			INTO Visitor (GUID, Name, VisitDate, WorkflowGUID)
			VALUES (@GUID, @Name, @VisitDate, @WorkflowGUID)

		SELECT @ID = SCOPE_IDENTITY();
COMMIT
BEGIN TRANSACTION

		INSERT 
			INTO Workflow (GUID, WorkflowName, WorkflowStateGUID, WorkflowData, WorkflowLog)
			VALUES (@WorkflowGUID, @WorkflowName, @WorkflowStateGUID, @WorkflowData, @WorkflowLog)
COMMIT
END
GO

CREATE PROCEDURE [dbo].[sp_Update_VisitorWorkflow]
@ID int,
@GUID uniqueidentifier,
@Name varchar(128),
@VisitDate smalldatetime,
@WorkflowGUID uniqueidentifier,
@WorkflowName varchar(128),
@WorkflowStateGUID uniqueidentifier,
@WorkflowData varchar(max),
@WorkflowLog varchar(max)

AS
BEGIN
		UPDATE Visitor 
            SET [Name] = @Name,	VisitDate = @VisitDate,	WorkFlowGUID = @WorkflowGUID
            WHERE ID = @ID

		UPDATE Workflow
			SET WorkflowName = @WorkflowName, WorkflowStateGUID = @WorkflowStateGUID, WorkflowData = @WorkflowData, WorkflowLog = @WorkflowLog
			WHERE GUID = @WorkflowGUID
END
GO
```
> Add Test data scriptt 

```sql
test data
```

```sql
```


## Data Classes

The code below shows a typical data class - this one if for user information.

Note:
1. It's a `Record` and implements the IDbRecord interface.  The properties that represent the database fields are *immutable*.
2. The class defines a set of static readonly string fields to define the property names we will use throughout the application in record  collections.
3. Attributes are used to define database fields and Stored Procedure Parameter information.
4. A DbRecordInfo object to define additional information about the record, such as the names used in the UI and the stored procedures to use for CUD operations.
5. A method to build a RecordCollection from the data properties to be used in Editor Components.
6. A method to build a new copy of the class from a RecordCollection. 

```c#
/// Implements the IDbRecord Interface
    public record DbUser : IDbRecord<DbUser>
    {
        /// A set of Static strings to define the names we will use throughout the
        public static readonly string __ID = "ID";
        public static readonly string __Name = "Name";
        public static readonly string __GUID = "GUID";
        public static readonly string __Groups = "Groups";
        public static readonly string __DisplayName = "DisplayName";

        [SPParameter(IsID = true, DataType = SqlDbType.Int)] public int ID { get; init; } = -1;
        [SPParameter(DataType = SqlDbType.UniqueIdentifier)] public Guid GUID { get; init; } = Guid.NewGuid();
        [SPParameter(DataType = SqlDbType.VarChar)] public string Name { get; init; } = string.Empty;
        [SPParameter(DataType = SqlDbType.VarChar)] public string Groups { get; init; } = string.Empty;

        [NotMapped] public string DisplayName => this.Name;
        [NotMapped] public DbRecordInfo RecordInfo => DbUser.RecInfo;

        [NotMapped]
        public static DbRecordInfo RecInfo =>
            new DbRecordInfo()
            {
                CreateSP = "sp_Add_User",
                UpdateSP = "sp_Update_User",
                DeleteSP = "sp_Delete_User",
                RecordDescription = "User",
                RecordName = "User",
                RecordListDescription = "Users",
                RecordListName = "Users"
            };
        
        public RecordCollection AsProperties() =>
            new RecordCollection()
            {
                { __ID, this.ID },
                { __Name, this.Name },
                { __Groups, this.Groups },
                { __GUID, this.GUID }
        };

        public static DbUser FromProperties(RecordCollection props) =>
            new DbUser()
            {
                ID = props.GetEditValue<int>(__ID),
                Name = props.GetEditValue<string>(__Name),
                GUID = props.GetEditValue<Guid>(__GUID)
            };

        public DbUser GetFromProperties(RecordCollection props) => DbUser.FromProperties(props);
    }
```
Workflow records also implement the `IWorkflowDbRecord` interface which defines the extra fields needed for workflow operations. 

```c#
    public interface IWorkflowDbRecord<TRecord>  
         where TRecord : class, IDbRecord<TRecord>, IWorkflowDbRecord<TRecord>, new()
    {
        public Guid WorkflowGUID { get; }
        public string WorkflowName { get;}
        public Guid WorkflowStateGUID { get;}
        public string WorkflowData { get; }
        public string WorkflowLog { get; }
        public string NewLog { get; }
    }
```

## Workflow Classes

### Workflow States

All workflow states implement `IWorkflowState`

```c#
    public delegate Task<WorkflowStateResult> WorkflowAction(bool run, PropertyCollection data);

    public interface IWorkflowState 
    {
        public Guid ID { get; }   //Individual GUID
        public Guid StateGUID { get;}   //State GUID
        public string Name { get; }   // Display Name

        public Action<Guid> ChangeStateAction { get; set; }  // Action Wired up by Workflow
        public WorkflowStateType StateType { get; }  // State type - Init/InProcess/End
        public List<WorkflowAction> StateTransitionActions { get;} // List of Actions to transition to a new state
        public List<WorkflowAction> InStateActions { get;}  // List of intra state Actions - e.g. save
        public List<WorkflowField> Fields { get; }  // list of fields with display options
        public WorkflowPermissions Permissions { get; }  // permissions object for the State

        // Save Action
        public Task<WorkflowStateResult> Save(bool run, PropertyCollection data = null) => Task.FromResult(WorkflowStateResult.NotActive());
    }
```

The base implementation is an abstract class.  Note:
1. The initialization process with the various virttual methods that can be overridden in inherited classes.  We'll see those in action below.
2. The inclusion of the Action to wire to `ChangeStateAction` in the initialization method.
3. The `ChangeStateAsync` Action that invokes the `ChangeStateAction`.

```c#
    public abstract class WorkflowStateBase : IWorkflowState
    {
        public Guid ID { get; protected set; } = Guid.NewGuid();
        public Guid StateGUID { get; set; } = Guid.Empty;
        public int StateID { get; set; } = 1;
        public string Name { get; protected set; }
        public WorkflowPermissions Permissions { get; init; } = new WorkflowPermissions();
        public Action<Guid> ChangeStateAction { get; set; }
        public virtual WorkflowStateType StateType { get; protected set; } = WorkflowStateType.None;
        public List<WorkflowAction> StateTransitionActions { get; protected set; } = new List<WorkflowAction>();
        public List<WorkflowAction> InStateActions { get; protected set; } = new List<WorkflowAction>();
        public List<WorkflowField> Fields { get; protected set; } = new List<WorkflowField>();

        public WorkflowStateBase(Action<Guid> changeStateTask, StateIDInfo info)
        {
            this.ChangeStateAction = changeStateTask;
            this.StateGUID = info.GUID;
            this.StateID = info.ID;
            this.Name = info.Name;
            this.StateType = info.StateType;
            this.LoadActions(loadActions);
            this.LoadFields(loadFields);
            this.LoadPermissions(loadPermissions);
        }

        protected virtual void LoadFields() { }
        protected virtual void LoadActions() { }
        protected virtual void LoadPermissions() { }

        public virtual async Task<WorkflowStateResult> Save(bool run, PropertyCollection data = null) => await ChangeStateAsync(Guid.Empty, true, data);

        public virtual Task<WorkflowStateResult> ChangeStateAsync(Guid stateguid, bool run, PropertyCollection data)
        {
            if (run) this.ChangeStateAction.Invoke(stateguid);
            return Task.FromResult(WorkflowStateResult.New(null));
        }

    }

```

`IVisitorWorkflowState` defines the specific Visitor Workflow State interface.  It defines a Task based method for all the Actions used in the workflow.

```c#
public interface IVisitorWorkflowState : IWorkflowState
    {

        public Task<WorkflowStateResult> Approve(bool run, PropertyCollection data = null) => Task.FromResult(WorkflowStateResult.NotActive());
        public Task<WorkflowStateResult> CheckIn(bool run, PropertyCollection data = null) => Task.FromResult(WorkflowStateResult.NotActive());
        public Task<WorkflowStateResult> CheckOut(bool run, PropertyCollection data = null) => Task.FromResult(WorkflowStateResult.NotActive());
        public Task<WorkflowStateResult> Close(bool run, PropertyCollection data = null) => Task.FromResult(WorkflowStateResult.NotActive());
        public Task<WorkflowStateResult> Reject(bool run, PropertyCollection data = null) => Task.FromResult(WorkflowStateResult.NotActive());
        public Task<WorkflowStateResult> ReOpen(bool run, PropertyCollection data = null) => Task.FromResult(WorkflowStateResult.NotActive());
        public Task<WorkflowStateResult> Submit(bool run, PropertyCollection data = null) => Task.FromResult(WorkflowStateResult.NotActive());
    }
```

```c#
public class WorkflowStateResult
{
    public bool IsActive { get; set; }
    public bool OK { get; set; }
    public object Data { get; set; } = null;
    
    public T GetData<T>() => this.Data != null && this.Data is T ? (T)Data : default;

    public bool TryGetData<T>(out T value) 
    {
        value = default;
        if (this.Data != null && this.Data is T) value = (T)Data;
        return EqualityComparer<T>.Default.Equals(value, default);
    }

    public static WorkflowStateResult NotActive() => new WorkflowStateResult() { IsActive = false, Data = null };
    public static WorkflowStateResult New(object data) => new WorkflowStateResult() { IsActive = true, Data = data };
    public static WorkflowStateResult New(bool ok) => new WorkflowStateResult() { IsActive = true, Data = null, OK = ok };

}
```

  Each method returns a `WorkflowStateResult`, shown below, with `IsActive` set to false.  This behaviour is used by the UI to determine if a button (linked to the Method) should be displayed.
  
  Overridden methods should return true.  The code snippets below are `Approve` and `ChangeStateAsync`.  Each Method calls `ChangeStateAsync` which invokes `ChangeStateAction` if `run` is true, or returns a `WorkflowStateResult` with `IsActive` true.  In the UI the *Approve* button is shown and linked to the `Approve` method.  We'll look end-to-end actions a little later.

```c#
/// From VisitorWorkflowState_Submitted
public async Task<WorkflowStateResult> Approve(bool run, PropertyCollection data) => await ChangeStateAsync(VisitorWorkflowState.State_Approved.GUID, true, data);

/// From WorkflowStateBase
public virtual Task<WorkflowStateResult> ChangeStateAsync(Guid stateguid, bool run, PropertyCollection data)
    {
        if (run) this.ChangeStateAction.Invoke(stateguid);
        return Task.FromResult(WorkflowStateResult.New(null));
    }
```

The base `VisitorWorkflowState` class implements most of the necessary functionality, and defines static information needed by the individual state classes. It:
1. Defines the static State data including the Guids used to uniquely identify each state.  They're defined  here togther for easy management. 
2. Implements `LoadFields`, `LoadActions` and `LoadPermissions` with the default settings for each of these.

```c#
public class VisitorWorkflowState : WorkflowStateBase, IVisitorWorkflowState
{
    // Static State Guid Information
    public static readonly StateIDInfo State_None = new StateIDInfo(Guid.Parse("71A15696-E922-4951-8FA5-91E7EB1E9161"), 1, "None", WorkflowStateType.Init);
    public static readonly StateIDInfo State_New = new StateIDInfo(Guid.Parse("32B187FB-C618-4456-AE33-642B5021C470"), 2, "New", WorkflowStateType.Init);
    public static readonly StateIDInfo State_Approved = new StateIDInfo(Guid.Parse("7F47FB46-F81B-46A3-A1BE-2D16F941EC9E"), 3, "Approved", WorkflowStateType.InProcess);
    public static readonly StateIDInfo State_CheckedIn = new StateIDInfo(Guid.Parse("6721F31D-DE1B-468F-950D-DFC14799AA67"), 4, "Checked In", WorkflowStateType.InProcess);
    public static readonly StateIDInfo State_CheckedOut = new StateIDInfo(Guid.Parse("8CC87FE8-D7AB-4C00-B3DF-203F0D144B94"), 5, "Checked Out", WorkflowStateType.InProcess);
    public static readonly StateIDInfo State_Closed = new StateIDInfo(Guid.Parse("EE7D7468-5EF4-40A6-AC9B-EA56F4C69F17"), 6, "Closed", WorkflowStateType.End);
    public static readonly StateIDInfo State_Rejected = new StateIDInfo(Guid.Parse("164C8FC0-578C-45DA-B40C-E079A4CD234A"), 7, "Rejected", WorkflowStateType.End);
    public static readonly StateIDInfo State_NoShow = new StateIDInfo(Guid.Parse("33FB1864-51DC-4458-B4E0-EFE3ECEDCC93"), 8, "No Show", WorkflowStateType.End);
    public static readonly StateIDInfo State_Submitted = new StateIDInfo(Guid.Parse("F33CFD53-D92D-4395-8738-1B826A9C9838"), 9, "Submitted", WorkflowStateType.InProcess);

    public VisitorWorkflowState(Action<Guid> action, StateIDInfo info) : base(action, info) 
    {
        // Set the default state GUID to New if it's not yet set
        if (this.StateGUID.Equals(Guid.Empty)) this.StateGUID = Guid.Parse("32B187FB-C618-4456-AE33-642B5021C470");
    }


    protected override void LoadFields()
    {
        this.Fields.Add(new WorkflowField() { FieldName = DbVisitorWorkflow.__ID, DisplayName = "Visitor ID", Display = FieldDisplayType.Display | FieldDisplayType.View });
        this.Fields.Add(new WorkflowField() { FieldName = DbVisitorWorkflow.__Name, DisplayName = "Visitor Name", Display = FieldDisplayType.Display | FieldDisplayType.View });
        this.Fields.Add(new WorkflowField() { FieldName = DbVisitorWorkflow.__VisitDate, DisplayName = "Visit Date", Display = FieldDisplayType.Display | FieldDisplayType.View });
        this.Fields.Add(new WorkflowField() { FieldName = DbVisitorWorkflow.__NewLog, DisplayName = "New Log", Display = FieldDisplayType.Edit | FieldDisplayType.View });
        this.Fields.Add(new WorkflowField() { FieldName = DbVisitorWorkflow.__WorkflowLog, DisplayName = "Workflow Log", Display = FieldDisplayType.Display | FieldDisplayType.View });
        this.Fields.Add(new WorkflowField() { FieldName = DbVisitorWorkflow.__WorkflowState, DisplayName = "Workflow State", Display = FieldDisplayType.Display | FieldDisplayType.View });
    }
    protected override void LoadActions()
    {
        this.InStateActions.Add(this.Save);
    }

    protected override void LoadDefaultPermissions()
        this.Permissions.Add(new WorkflowPermission(GroupInfo.Group_User, WorkflowPermissionType.Read));
        this.Permissions.Add(new WorkflowPermission(GroupInfo.Group_VisitorAdmins, WorkflowPermissionType.ReadWrite));
    }
}
```
We finally get to the real state objects.  `VisitorWorkflowState_New`,  `VisitorWorkflowState_Approved` and `VisitorWorkflowState_NoShow` are shown below.

Note:
1. They call the base class initialization `new` method with the specific `StateIDInfo` object for the State.
2. They override `LoadFields`, `LoadPermissions` and `LoadActions` as needed.
3. They implement their specific *Actions*.

```c#
    public VisitorWorkflowState_New(Action<Guid> changestateaction) : base(changestateaction, State_New) { }

    protected override void LoadActions()
    {
        base.LoadActions();
        this.StateTransitionActions.Add(this.Submit);
    }

    protected override void LoadFields()
    {
        this.Fields.Add(new WorkflowField() { FieldName = DbVisitorWorkflow.__Name, DisplayName = "Visitor Name", Display = FieldDisplayType.Edit | FieldDisplayType.View });
        this.Fields.Add(new WorkflowField() { FieldName = DbVisitorWorkflow.__VisitDate, DisplayName = "Visit Date", Display = FieldDisplayType.Edit | FieldDisplayType.View });
        this.Fields.Add(new WorkflowField() { FieldName = DbVisitorWorkflow.__NewLog, DisplayName = "New Log", Display = FieldDisplayType.Edit | FieldDisplayType.View });
        this.Fields.Add(new WorkflowField() { FieldName = DbVisitorWorkflow.__WorkflowLog, DisplayName = "Workflow Log", Display = FieldDisplayType.Display | FieldDisplayType.View });
        this.Fields.Add(new WorkflowField() { FieldName = DbVisitorWorkflow.__WorkflowState, DisplayName = "Workflow State", Display = FieldDisplayType.Display | FieldDisplayType.View });
    }

    protected override void LoadPermissions()
    {
        base.LoadPermissions();
        this.Permissions.Add(new WorkflowPermission(GroupInfo.Group_User, WorkflowPermissionType.ReadWrite));
    }

    public async Task<WorkflowStateResult> Submit(bool run, PropertyCollection data) => await ChangeStateAsync(VisitorWorkflowState.State_Submitted.GUID, true, data);
}
```
```c#
    public class VisitorWorkflowState_Approved : VisitorWorkflowState, IVisitorWorkflowState
    {
        public VisitorWorkflowState_Approved(Action<Guid> changestateaction) : base(changestateaction, State_Approved) { }

        protected override void LoadActions()
        {
            base.LoadActions();
            this.StateTransitionActions.Add(this.NoShow);
            this.StateTransitionActions.Add(this.CheckIn);
        }
        public async Task<WorkflowStateResult> NoShow(bool run, PropertyCollection data) => await ChangeStateAsync(VisitorWorkflowState.State_NoShow.GUID, true, data);

        public async Task<WorkflowStateResult> CheckIn(bool run, PropertyCollection data) => await ChangeStateAsync(VisitorWorkflowState.State_CheckedIn.GUID, true, data);
    }
```
```c#
public class VisitorWorkflowState_NoShow : VisitorWorkflowState, IVisitorWorkflowState
{
    public VisitorWorkflowState_NoShow(Action<Guid> changestateaction) : base(changestateaction, State_NoShow) { }
}
```




1. Set up a database table for the workflow data.
2. Set up the View and Create/Update Stored Procedures.
3. Create the model DB class.
4. Create a Workflow State Interface.
5. Create a Workflow State base class.
6. Create the Individual state classes.
7. Add any new states to the database.
8. Create a Workflow Control Service.
9. Add the Workflow States to the Workflow Control Service.
10. Set up the Workflow Control Service. 

