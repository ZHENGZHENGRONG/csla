<Serializable()> _
Public Class Project
  Inherits BusinessBase(Of Project)

#Region " Business Methods "

  Private mTimestamp(7) As Byte

  Private Shared IdProperty As PropertyInfo(Of Guid) = RegisterProperty(Of Guid, Project)("Id")
  Private mId As Guid = Guid.NewGuid
  <System.ComponentModel.DataObjectField(True, True)> _
  Public ReadOnly Property Id() As Guid
    Get
      Return GetProperty(Of Guid)(IdProperty, Mid)
    End Get
  End Property

  Private Shared NameProperty As PropertyInfo(Of String) = RegisterProperty(Of String, Project)("Name")
  Private mName As String = NameProperty.DefaultValue
  Public Property Name() As String
    Get
      Return GetProperty(Of String)(NameProperty, mName)
    End Get
    Set(ByVal value As String)
      SetProperty(Of String)(NameProperty, mName, value)
    End Set
  End Property

  Private Shared StartedProperty As PropertyInfo(Of SmartDate) = RegisterProperty(Of SmartDate, Project)("Started")
  Private mStarted As SmartDate = StartedProperty.DefaultValue
  Public Property Started() As String
    Get
      Return GetProperty(Of SmartDate)(StartedProperty, mStarted)
    End Get
    Set(ByVal value As String)
      SetProperty(Of SmartDate)(StartedProperty, mStarted, value)
    End Set
  End Property

  Private Shared EndedProperty As PropertyInfo(Of SmartDate) = RegisterProperty(Of SmartDate, Project)("Ended", New SmartDate(False))
  Private mEnded As SmartDate = EndedProperty.DefaultValue
  Public Property Ended() As String
    Get
      Return GetProperty(Of SmartDate)(EndedProperty, mEnded)
    End Get
    Set(ByVal value As String)
      SetProperty(Of SmartDate)(EndedProperty, mEnded, value)
    End Set
  End Property

  Private Shared DescriptionProperty As PropertyInfo(Of String) = RegisterProperty(Of String, Project)("Description")
  Private mDescription As String = DescriptionProperty.DefaultValue
  Public Property Description() As String
    Get
      Return GetProperty(Of String)(DescriptionProperty, mDescription)
    End Get
    Set(ByVal value As String)
      SetProperty(Of String)(DescriptionProperty, mDescription, value)
    End Set
  End Property

  Private Shared ResourcesProperty As PropertyInfo(Of ProjectResources) = RegisterProperty(Of ProjectResources, Project)("Resources")
  Public ReadOnly Property Resources() As ProjectResources
    Get
      If Not ChildExists(ResourcesProperty) Then
        SetChild(Of ProjectResources)(ResourcesProperty, ProjectResources.NewProjectResources())
      End If
      Return GetChild(Of ProjectResources)(ResourcesProperty)
    End Get
  End Property

  Public Overrides Function ToString() As String
    Return mId.ToString
  End Function

#End Region

#Region " Validation Rules "

  Protected Overrides Sub AddBusinessRules()

    ValidationRules.AddRule( _
      AddressOf Validation.CommonRules.StringRequired, New Csla.Validation.RuleArgs("Name", "Project name"))
    ValidationRules.AddRule( _
      AddressOf Validation.CommonRules.StringMaxLength, _
      New Validation.CommonRules.MaxLengthRuleArgs("Name", 50))

    ValidationRules.AddRule(Of Project)(AddressOf StartDateGTEndDate(Of Project), "Started")
    ValidationRules.AddRule(Of Project)(AddressOf StartDateGTEndDate(Of Project), "Ended")

    ValidationRules.AddDependantProperty("Started", "Ended", True)

  End Sub

  Private Shared Function StartDateGTEndDate(Of T As Project)( _
    ByVal target As T, _
    ByVal e As Validation.RuleArgs) As Boolean

    If target.mStarted > target.mEnded Then
      e.Description = "Start date can't be after end date"
      Return False

    Else
      Return True
    End If

  End Function

#End Region

#Region " Authorization Rules "

  Protected Overrides Sub AddAuthorizationRules()

    ' add AuthorizationRules here
    AuthorizationRules.AllowWrite("Name", "ProjectManager")
    AuthorizationRules.AllowWrite("Started", "ProjectManager")
    AuthorizationRules.AllowWrite("Ended", "ProjectManager")
    AuthorizationRules.AllowWrite("Description", "ProjectManager")

  End Sub

  Public Shared Function CanAddObject() As Boolean

    Return Csla.ApplicationContext.User.IsInRole("ProjectManager")

  End Function

  Public Shared Function CanGetObject() As Boolean

    Return True

  End Function

  Public Shared Function CanDeleteObject() As Boolean

    Dim result As Boolean
    If Csla.ApplicationContext.User.IsInRole("ProjectManager") Then
      result = True
    End If
    If Csla.ApplicationContext.User.IsInRole("Administrator") Then
      result = True
    End If
    Return result

  End Function

  Public Shared Function CanEditObject() As Boolean

    Return Csla.ApplicationContext.User.IsInRole("ProjectManager")

  End Function

#End Region

#Region " Factory Methods "

  Public Shared Function NewProject() As Project

    If Not CanAddObject() Then
      Throw New System.Security.SecurityException( _
        "User not authorized to add a project")
    End If
    Return DataPortal.Create(Of Project)()

  End Function

  Public Shared Function GetProject(ByVal id As Guid) As Project

    If Not CanGetObject() Then
      Throw New System.Security.SecurityException( _
        "User not authorized to view a project")
    End If
    Return DataPortal.Fetch(Of Project)(New Criteria(id))

  End Function

  Public Shared Sub DeleteProject(ByVal id As Guid)

    If Not CanDeleteObject() Then
      Throw New System.Security.SecurityException( _
        "User not authorized to remove a project")
    End If
    DataPortal.Delete(New Criteria(id))

  End Sub

  Public Overrides Function Save() As Project

    If IsDeleted AndAlso Not CanDeleteObject() Then
      Throw New System.Security.SecurityException( _
        "User not authorized to remove a project")

    ElseIf IsNew AndAlso Not CanAddObject() Then
      Throw New System.Security.SecurityException( _
        "User not authorized to add a project")

    ElseIf Not CanEditObject() Then
      Throw New System.Security.SecurityException( _
        "User not authorized to update a project")
    End If
    Return MyBase.Save

  End Function

  Private Sub New()
    ' require use of factory methods
  End Sub

#End Region

#Region " Data Access "

  <Serializable()> _
  Private Class Criteria

    Private mId As Guid
    Public ReadOnly Property Id() As Guid
      Get
        Return mId
      End Get
    End Property

    Public Sub New(ByVal id As Guid)
      mId = id
    End Sub
  End Class

  <RunLocal()> _
  Protected Overrides Sub DataPortal_Create()

    mId = Guid.NewGuid
    Started = CStr(Today)
    ValidationRules.CheckRules()

  End Sub

  Private Overloads Sub DataPortal_Fetch(ByVal criteria As Criteria)

    Using cn As New SqlConnection(Database.PTrackerConnection)
      cn.Open()
      Using cm As SqlCommand = cn.CreateCommand
        cm.CommandType = CommandType.StoredProcedure
        cm.CommandText = "getProject"
        cm.Parameters.AddWithValue("@id", criteria.Id)

        Using dr As New SafeDataReader(cm.ExecuteReader)
          dr.Read()
          With dr
            mId = .GetGuid("Id")
            mName = .GetString("Name")
            mStarted = .GetSmartDate("Started", mStarted.EmptyIsMin)
            mEnded = .GetSmartDate("Ended", mEnded.EmptyIsMin)
            mDescription = .GetString("Description")
            .GetBytes("LastChanged", 0, mTimestamp, 0, 8)

            ' load child objects
            .NextResult()
            SetChild(Of ProjectResources)(ResourcesProperty, ProjectResources.GetProjectResources(dr))
          End With
        End Using
      End Using
    End Using

  End Sub

  <Transactional(TransactionalTypes.TransactionScope)> _
  Protected Overrides Sub DataPortal_Insert()

    Using cn As New SqlConnection(Database.PTrackerConnection)
      cn.Open()
      Using cm As SqlCommand = cn.CreateCommand
        cm.CommandText = "addProject"
        DoInsertUpdate(cm)
      End Using
    End Using
    ' update child objects
    GetChild(Of ProjectResources)(ResourcesProperty).Update(Me)

  End Sub

  <Transactional(TransactionalTypes.TransactionScope)> _
  Protected Overrides Sub DataPortal_Update()

    If MyBase.IsDirty Then
      Using cn As New SqlConnection(Database.PTrackerConnection)
        cn.Open()
        Using cm As SqlCommand = cn.CreateCommand
          cm.CommandText = "updateProject"
          cm.Parameters.AddWithValue("@lastChanged", mTimestamp)
          DoInsertUpdate(cm)
        End Using
      End Using
    End If
    ' update child objects
    GetChild(Of ProjectResources)(ResourcesProperty).Update(Me)

  End Sub

  Private Sub DoInsertUpdate(ByVal cm As SqlCommand)

    With cm
      .CommandType = CommandType.StoredProcedure
      .Parameters.AddWithValue("@id", mId)
      .Parameters.AddWithValue("@name", mName)
      .Parameters.AddWithValue("@started", mStarted.DBValue)
      .Parameters.AddWithValue("@ended", mEnded.DBValue)
      .Parameters.AddWithValue("@description", mDescription)
      Dim param As New SqlParameter("@newLastChanged", SqlDbType.Timestamp)
      param.Direction = ParameterDirection.Output
      .Parameters.Add(param)

      .ExecuteNonQuery()

      mTimestamp = CType(.Parameters("@newLastChanged").Value, Byte())
    End With

  End Sub

  <Transactional(TransactionalTypes.TransactionScope)> _
  Protected Overrides Sub DataPortal_DeleteSelf()

    DataPortal_Delete(New Criteria(mId))

  End Sub

  <Transactional(TransactionalTypes.TransactionScope)> _
  Private Overloads Sub DataPortal_Delete(ByVal criteria As Criteria)

    Using cn As New SqlConnection(Database.PTrackerConnection)
      cn.Open()
      Using cm As SqlCommand = cn.CreateCommand
        With cm
          .Connection = cn
          .CommandType = CommandType.StoredProcedure
          .CommandText = "deleteProject"
          .Parameters.AddWithValue("@id", criteria.Id)
          .ExecuteNonQuery()
        End With
      End Using
    End Using

  End Sub

#End Region

#Region " Exists "

  Public Shared Function Exists(ByVal id As Guid) As Boolean

    Return ExistsCommand.Exists(id)

  End Function

  <Serializable()> _
  Private Class ExistsCommand
    Inherits CommandBase

    Private mId As Guid
    Private mExists As Boolean

    Public ReadOnly Property ProjectExists() As Boolean
      Get
        Return mExists
      End Get
    End Property

    Public Shared Function Exists(ByVal id As Guid) As Boolean

      Dim result As ExistsCommand
      result = DataPortal.Execute(Of ExistsCommand)(New ExistsCommand(id))
      Return result.ProjectExists

    End Function

    Private Sub New(ByVal id As Guid)
      mId = id
    End Sub

    Protected Overrides Sub DataPortal_Execute()

      Using cn As New SqlConnection(Database.PTrackerConnection)
        cn.Open()
        Using cm As SqlCommand = cn.CreateCommand
          cm.CommandType = CommandType.Text
          cm.CommandText = "SELECT Id FROM Projects WHERE Id=@id"
          cm.Parameters.AddWithValue("@id", mId)

          Dim count As Integer = CInt(cm.ExecuteScalar)
          mExists = (count > 0)
        End Using
      End Using

    End Sub

  End Class

#End Region

End Class
