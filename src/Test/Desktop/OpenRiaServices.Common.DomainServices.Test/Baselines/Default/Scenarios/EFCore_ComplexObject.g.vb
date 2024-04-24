'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports EFCoreModels.Scenarios.OwnedTypes
Imports OpenRiaServices
Imports OpenRiaServices.Client
Imports OpenRiaServices.Client.Authentication
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Linq
Imports System.Runtime.Serialization
Imports System.ServiceModel
Imports System.Threading.Tasks

Namespace EFCoreModels.Scenarios.OwnedTypes
    
    ''' <summary>
    ''' The 'Address' class.
    ''' </summary>
    <DataContract([Namespace]:="http://schemas.datacontract.org/2004/07/EFCoreModels.Scenarios.OwnedTypes")>  _
    Partial Public NotInheritable Class Address
        Inherits ComplexObject
        
        Private _addressLine As String
        
        Private _city As String
        
        #Region "Extensibility Method Definitions"

        ''' <summary>
        ''' This method is invoked from the constructor once initialization is complete and
        ''' can be used for further object setup.
        ''' </summary>
        Private Partial Sub OnCreated()
        End Sub
        Private Partial Sub OnAddressLineChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnAddressLineChanged()
        End Sub
        Private Partial Sub OnCityChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnCityChanged()
        End Sub

        #End Region
        
        
        ''' <summary>
        ''' Initializes a new instance of the <see cref="Address"/> class.
        ''' </summary>
        Public Sub New()
            MyBase.New
            Me.OnCreated
        End Sub
        
        ''' <summary>
        ''' Gets or sets the 'AddressLine' value.
        ''' </summary>
        <DataMember(),  _
         Required(),  _
         StringLength(100)>  _
        Public Property AddressLine() As String
            Get
                Return Me._addressLine
            End Get
            Set
                If (String.Equals(Me._addressLine, value) = false) Then
                    Me.OnAddressLineChanging(value)
                    Me.RaiseDataMemberChanging("AddressLine")
                    Me.ValidateProperty("AddressLine", value)
                    Me._addressLine = value
                    Me.RaiseDataMemberChanged("AddressLine")
                    Me.OnAddressLineChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the 'City' value.
        ''' </summary>
        <DataMember(),  _
         Required(),  _
         StringLength(50)>  _
        Public Property City() As String
            Get
                Return Me._city
            End Get
            Set
                If (String.Equals(Me._city, value) = false) Then
                    Me.OnCityChanging(value)
                    Me.RaiseDataMemberChanging("City")
                    Me.ValidateProperty("City", value)
                    Me._city = value
                    Me.RaiseDataMemberChanged("City")
                    Me.OnCityChanged
                End If
            End Set
        End Property
    End Class
    
    ''' <summary>
    ''' The 'ContactInfo' class.
    ''' </summary>
    <DataContract([Namespace]:="http://schemas.datacontract.org/2004/07/EFCoreModels.Scenarios.OwnedTypes")>  _
    Partial Public NotInheritable Class ContactInfo
        Inherits ComplexObject
        
        Private _address As Address
        
        Private _homePhone As String
        
        #Region "Extensibility Method Definitions"

        ''' <summary>
        ''' This method is invoked from the constructor once initialization is complete and
        ''' can be used for further object setup.
        ''' </summary>
        Private Partial Sub OnCreated()
        End Sub
        Private Partial Sub OnAddressChanging(ByVal value As Address)
        End Sub
        Private Partial Sub OnAddressChanged()
        End Sub
        Private Partial Sub OnHomePhoneChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnHomePhoneChanged()
        End Sub

        #End Region
        
        
        ''' <summary>
        ''' Initializes a new instance of the <see cref="ContactInfo"/> class.
        ''' </summary>
        Public Sub New()
            MyBase.New
            Me.OnCreated
        End Sub
        
        ''' <summary>
        ''' Gets or sets the 'Address' value.
        ''' </summary>
        <DataMember(),  _
         Display(AutoGenerateField:=false)>  _
        Public Property Address() As Address
            Get
                Return Me._address
            End Get
            Set
                If (Object.Equals(Me._address, value) = false) Then
                    Me.OnAddressChanging(value)
                    Me.RaiseDataMemberChanging("Address")
                    Me.ValidateProperty("Address", value)
                    Me._address = value
                    Me.RaiseDataMemberChanged("Address")
                    Me.OnAddressChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the 'HomePhone' value.
        ''' </summary>
        <DataMember(),  _
         Required(),  _
         StringLength(24)>  _
        Public Property HomePhone() As String
            Get
                Return Me._homePhone
            End Get
            Set
                If (String.Equals(Me._homePhone, value) = false) Then
                    Me.OnHomePhoneChanging(value)
                    Me.RaiseDataMemberChanging("HomePhone")
                    Me.ValidateProperty("HomePhone", value)
                    Me._homePhone = value
                    Me.RaiseDataMemberChanged("HomePhone")
                    Me.OnHomePhoneChanged
                End If
            End Set
        End Property
    End Class
    
    ''' <summary>
    ''' The 'Employee' entity class.
    ''' </summary>
    <DataContract([Namespace]:="http://schemas.datacontract.org/2004/07/EFCoreModels.Scenarios.OwnedTypes")>  _
    Partial Public NotInheritable Class Employee
        Inherits Entity
        
        Private _contactInfo As ContactInfo
        
        Private _employeeId As Integer
        
        Private _ownedEntityWithBackNavigation As OwnedEntityWithBackNavigation
        
        Private _ownedEntityWithExplicitId As EntityRef(Of OwnedEntityWithExplicitId)
        
        Private _ownedEntityWithExplicitIdAndBackNavigation As EntityRef(Of OwnedEntityWithExplicitIdAndBackNavigation)
        
        #Region "Extensibility Method Definitions"

        ''' <summary>
        ''' This method is invoked from the constructor once initialization is complete and
        ''' can be used for further object setup.
        ''' </summary>
        Private Partial Sub OnCreated()
        End Sub
        Private Partial Sub OnContactInfoChanging(ByVal value As ContactInfo)
        End Sub
        Private Partial Sub OnContactInfoChanged()
        End Sub
        Private Partial Sub OnEmployeeIdChanging(ByVal value As Integer)
        End Sub
        Private Partial Sub OnEmployeeIdChanged()
        End Sub
        Private Partial Sub OnOwnedEntityWithBackNavigationChanging(ByVal value As OwnedEntityWithBackNavigation)
        End Sub
        Private Partial Sub OnOwnedEntityWithBackNavigationChanged()
        End Sub

        #End Region
        
        
        ''' <summary>
        ''' Initializes a new instance of the <see cref="Employee"/> class.
        ''' </summary>
        Public Sub New()
            MyBase.New
            Me.OnCreated
        End Sub
        
        ''' <summary>
        ''' Gets or sets the 'ContactInfo' value.
        ''' </summary>
        <DataMember(),  _
         Display(AutoGenerateField:=false)>  _
        Public Property ContactInfo() As ContactInfo
            Get
                Return Me._contactInfo
            End Get
            Set
                If (Object.Equals(Me._contactInfo, value) = false) Then
                    Me.OnContactInfoChanging(value)
                    Me.RaiseDataMemberChanging("ContactInfo")
                    Me.ValidateProperty("ContactInfo", value)
                    Me._contactInfo = value
                    Me.RaiseDataMemberChanged("ContactInfo")
                    Me.OnContactInfoChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the 'EmployeeId' value.
        ''' </summary>
        <DataMember(),  _
         Editable(false, AllowInitialValue:=true),  _
         Key(),  _
         RoundtripOriginal()>  _
        Public Property EmployeeId() As Integer
            Get
                Return Me._employeeId
            End Get
            Set
                If ((Me._employeeId = value)  _
                            = false) Then
                    Me.OnEmployeeIdChanging(value)
                    Me.ValidateProperty("EmployeeId", value)
                    Me._employeeId = value
                    Me.RaisePropertyChanged("EmployeeId")
                    Me.OnEmployeeIdChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the 'OwnedEntityWithBackNavigation' value.
        ''' </summary>
        <DataMember(),  _
         Display(AutoGenerateField:=false)>  _
        Public Property OwnedEntityWithBackNavigation() As OwnedEntityWithBackNavigation
            Get
                Return Me._ownedEntityWithBackNavigation
            End Get
            Set
                If (Object.Equals(Me._ownedEntityWithBackNavigation, value) = false) Then
                    Me.OnOwnedEntityWithBackNavigationChanging(value)
                    Me.RaiseDataMemberChanging("OwnedEntityWithBackNavigation")
                    Me.ValidateProperty("OwnedEntityWithBackNavigation", value)
                    Me._ownedEntityWithBackNavigation = value
                    Me.RaiseDataMemberChanged("OwnedEntityWithBackNavigation")
                    Me.OnOwnedEntityWithBackNavigationChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the associated <see cref="OwnedEntityWithExplicitId"/> entity.
        ''' </summary>
        <Association("FK_Employees_Employees_EmployeeId|owns:OwnedEntityWithExplicitId", "EmployeeId", "EmployeeId")>  _
        Public Property OwnedEntityWithExplicitId() As OwnedEntityWithExplicitId
            Get
                If (Me._ownedEntityWithExplicitId Is Nothing) Then
                    Me._ownedEntityWithExplicitId = New EntityRef(Of OwnedEntityWithExplicitId)(Me, "OwnedEntityWithExplicitId", AddressOf Me.FilterOwnedEntityWithExplicitId)
                End If
                Return Me._ownedEntityWithExplicitId.Entity
            End Get
            Set
                Dim previous As OwnedEntityWithExplicitId = Me.OwnedEntityWithExplicitId
                If (Object.Equals(previous, value) = false) Then
                    Me.ValidateProperty("OwnedEntityWithExplicitId", value)
                    Me._ownedEntityWithExplicitId.Entity = value
                    Me.RaisePropertyChanged("OwnedEntityWithExplicitId")
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the associated <see cref="OwnedEntityWithExplicitIdAndBackNavigation"/> entity.
        ''' </summary>
        <Association("FK_Employees_Employees_EmployeeId|owns:OwnedEntityWithExplicitIdAndBackNavigation"& _ 
            "", "EmployeeId", "EmployeeId")>  _
        Public Property OwnedEntityWithExplicitIdAndBackNavigation() As OwnedEntityWithExplicitIdAndBackNavigation
            Get
                If (Me._ownedEntityWithExplicitIdAndBackNavigation Is Nothing) Then
                    Me._ownedEntityWithExplicitIdAndBackNavigation = New EntityRef(Of OwnedEntityWithExplicitIdAndBackNavigation)(Me, "OwnedEntityWithExplicitIdAndBackNavigation", AddressOf Me.FilterOwnedEntityWithExplicitIdAndBackNavigation)
                End If
                Return Me._ownedEntityWithExplicitIdAndBackNavigation.Entity
            End Get
            Set
                Dim previous As OwnedEntityWithExplicitIdAndBackNavigation = Me.OwnedEntityWithExplicitIdAndBackNavigation
                If (Object.Equals(previous, value) = false) Then
                    Me.ValidateProperty("OwnedEntityWithExplicitIdAndBackNavigation", value)
                    Me._ownedEntityWithExplicitIdAndBackNavigation.Entity = value
                    Me.RaisePropertyChanged("OwnedEntityWithExplicitIdAndBackNavigation")
                End If
            End Set
        End Property
        
        Private Function FilterOwnedEntityWithExplicitId(ByVal entity As OwnedEntityWithExplicitId) As Boolean
            Return Object.Equals(entity.EmployeeId, Me.EmployeeId)
        End Function
        
        Private Function FilterOwnedEntityWithExplicitIdAndBackNavigation(ByVal entity As OwnedEntityWithExplicitIdAndBackNavigation) As Boolean
            Return Object.Equals(entity.EmployeeId, Me.EmployeeId)
        End Function
        
        ''' <summary>
        ''' Computes a value from the key fields that uniquely identifies this entity instance.
        ''' </summary>
        ''' <returns>An object instance that uniquely identifies this entity instance.</returns>
        Public Overrides Function GetIdentity() As Object
            Return Me._employeeId
        End Function
    End Class
    
    ''' <summary>
    ''' The 'OwnedEntityWithBackNavigation' class.
    ''' </summary>
    <DataContract([Namespace]:="http://schemas.datacontract.org/2004/07/EFCoreModels.Scenarios.OwnedTypes")>  _
    Partial Public NotInheritable Class OwnedEntityWithBackNavigation
        Inherits ComplexObject
        
        Private _description As String
        
        #Region "Extensibility Method Definitions"

        ''' <summary>
        ''' This method is invoked from the constructor once initialization is complete and
        ''' can be used for further object setup.
        ''' </summary>
        Private Partial Sub OnCreated()
        End Sub
        Private Partial Sub OnDescriptionChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnDescriptionChanged()
        End Sub

        #End Region
        
        
        ''' <summary>
        ''' Initializes a new instance of the <see cref="OwnedEntityWithBackNavigation"/> class.
        ''' </summary>
        Public Sub New()
            MyBase.New
            Me.OnCreated
        End Sub
        
        ''' <summary>
        ''' Gets or sets the 'Description' value.
        ''' </summary>
        <DataMember(),  _
         Required()>  _
        Public Property Description() As String
            Get
                Return Me._description
            End Get
            Set
                If (String.Equals(Me._description, value) = false) Then
                    Me.OnDescriptionChanging(value)
                    Me.RaiseDataMemberChanging("Description")
                    Me.ValidateProperty("Description", value)
                    Me._description = value
                    Me.RaiseDataMemberChanged("Description")
                    Me.OnDescriptionChanged
                End If
            End Set
        End Property
    End Class
    
    ''' <summary>
    ''' The 'OwnedEntityWithExplicitId' entity class.
    ''' </summary>
    <DataContract([Namespace]:="http://schemas.datacontract.org/2004/07/EFCoreModels.Scenarios.OwnedTypes")>  _
    Partial Public NotInheritable Class OwnedEntityWithExplicitId
        Inherits Entity
        
        Private _description As String
        
        Private _employeeId As Integer
        
        #Region "Extensibility Method Definitions"

        ''' <summary>
        ''' This method is invoked from the constructor once initialization is complete and
        ''' can be used for further object setup.
        ''' </summary>
        Private Partial Sub OnCreated()
        End Sub
        Private Partial Sub OnDescriptionChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnDescriptionChanged()
        End Sub
        Private Partial Sub OnEmployeeIdChanging(ByVal value As Integer)
        End Sub
        Private Partial Sub OnEmployeeIdChanged()
        End Sub

        #End Region
        
        
        ''' <summary>
        ''' Initializes a new instance of the <see cref="OwnedEntityWithExplicitId"/> class.
        ''' </summary>
        Public Sub New()
            MyBase.New
            Me.OnCreated
        End Sub
        
        ''' <summary>
        ''' Gets or sets the 'Description' value.
        ''' </summary>
        <DataMember(),  _
         Required()>  _
        Public Property Description() As String
            Get
                Return Me._description
            End Get
            Set
                If (String.Equals(Me._description, value) = false) Then
                    Me.OnDescriptionChanging(value)
                    Me.RaiseDataMemberChanging("Description")
                    Me.ValidateProperty("Description", value)
                    Me._description = value
                    Me.RaiseDataMemberChanged("Description")
                    Me.OnDescriptionChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the 'EmployeeId' value.
        ''' </summary>
        <DataMember(),  _
         Editable(false, AllowInitialValue:=true),  _
         Key(),  _
         RoundtripOriginal()>  _
        Public Property EmployeeId() As Integer
            Get
                Return Me._employeeId
            End Get
            Set
                If ((Me._employeeId = value)  _
                            = false) Then
                    Me.OnEmployeeIdChanging(value)
                    Me.ValidateProperty("EmployeeId", value)
                    Me._employeeId = value
                    Me.RaisePropertyChanged("EmployeeId")
                    Me.OnEmployeeIdChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Computes a value from the key fields that uniquely identifies this entity instance.
        ''' </summary>
        ''' <returns>An object instance that uniquely identifies this entity instance.</returns>
        Public Overrides Function GetIdentity() As Object
            Return Me._employeeId
        End Function
    End Class
    
    ''' <summary>
    ''' The 'OwnedEntityWithExplicitIdAndBackNavigation' entity class.
    ''' </summary>
    <DataContract([Namespace]:="http://schemas.datacontract.org/2004/07/EFCoreModels.Scenarios.OwnedTypes")>  _
    Partial Public NotInheritable Class OwnedEntityWithExplicitIdAndBackNavigation
        Inherits Entity
        
        Private _description As String
        
        Private _employee As EntityRef(Of Employee)
        
        Private _employeeId As Integer
        
        #Region "Extensibility Method Definitions"

        ''' <summary>
        ''' This method is invoked from the constructor once initialization is complete and
        ''' can be used for further object setup.
        ''' </summary>
        Private Partial Sub OnCreated()
        End Sub
        Private Partial Sub OnDescriptionChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnDescriptionChanged()
        End Sub
        Private Partial Sub OnEmployeeIdChanging(ByVal value As Integer)
        End Sub
        Private Partial Sub OnEmployeeIdChanged()
        End Sub

        #End Region
        
        
        ''' <summary>
        ''' Initializes a new instance of the <see cref="OwnedEntityWithExplicitIdAndBackNavigation"/> class.
        ''' </summary>
        Public Sub New()
            MyBase.New
            Me.OnCreated
        End Sub
        
        ''' <summary>
        ''' Gets or sets the 'Description' value.
        ''' </summary>
        <DataMember(),  _
         Required()>  _
        Public Property Description() As String
            Get
                Return Me._description
            End Get
            Set
                If (String.Equals(Me._description, value) = false) Then
                    Me.OnDescriptionChanging(value)
                    Me.RaiseDataMemberChanging("Description")
                    Me.ValidateProperty("Description", value)
                    Me._description = value
                    Me.RaiseDataMemberChanged("Description")
                    Me.OnDescriptionChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the associated <see cref="Employee"/> entity.
        ''' </summary>
        <Association("FK_Employees_Employees_EmployeeId", "EmployeeId", "EmployeeId", IsForeignKey:=true)>  _
        Public Property Employee() As Employee
            Get
                If (Me._employee Is Nothing) Then
                    Me._employee = New EntityRef(Of Employee)(Me, "Employee", AddressOf Me.FilterEmployee)
                End If
                Return Me._employee.Entity
            End Get
            Set
                Dim previous As Employee = Me.Employee
                If (Object.Equals(previous, value) = false) Then
                    Me.ValidateProperty("Employee", value)
                    If (Not (value) Is Nothing) Then
                        Me.EmployeeId = value.EmployeeId
                    Else
                        Me.EmployeeId = CType(Nothing, Integer)
                    End If
                    Me._employee.Entity = value
                    Me.RaisePropertyChanged("Employee")
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the 'EmployeeId' value.
        ''' </summary>
        <DataMember(),  _
         Editable(false, AllowInitialValue:=true),  _
         Key(),  _
         RoundtripOriginal()>  _
        Public Property EmployeeId() As Integer
            Get
                Return Me._employeeId
            End Get
            Set
                If ((Me._employeeId = value)  _
                            = false) Then
                    Me.OnEmployeeIdChanging(value)
                    Me.ValidateProperty("EmployeeId", value)
                    Me._employeeId = value
                    Me.RaisePropertyChanged("EmployeeId")
                    Me.OnEmployeeIdChanged
                End If
            End Set
        End Property
        
        Private Function FilterEmployee(ByVal entity As Employee) As Boolean
            Return Object.Equals(entity.EmployeeId, Me.EmployeeId)
        End Function
        
        ''' <summary>
        ''' Computes a value from the key fields that uniquely identifies this entity instance.
        ''' </summary>
        ''' <returns>An object instance that uniquely identifies this entity instance.</returns>
        Public Overrides Function GetIdentity() As Object
            Return Me._employeeId
        End Function
    End Class
End Namespace

Namespace OpenRiaServices.Tools.Test
    
    ''' <summary>
    ''' The DomainContext corresponding to the 'EFCoreComplexTypesService' DomainService.
    ''' </summary>
    Partial Public NotInheritable Class EFCoreComplexTypesContext
        Inherits DomainContext
        
        #Region "Extensibility Method Definitions"

        ''' <summary>
        ''' This method is invoked from the constructor once initialization is complete and
        ''' can be used for further object setup.
        ''' </summary>
        Private Partial Sub OnCreated()
        End Sub

        #End Region
        
        
        ''' <summary>
        ''' Initializes a new instance of the <see cref="EFCoreComplexTypesContext"/> class.
        ''' </summary>
        Public Sub New()
            Me.New(New Uri("OpenRiaServices-Tools-Test-EFCoreComplexTypesService.svc", UriKind.Relative))
        End Sub
        
        ''' <summary>
        ''' Initializes a new instance of the <see cref="EFCoreComplexTypesContext"/> class with the specified service URI.
        ''' </summary>
        ''' <param name="serviceUri">The EFCoreComplexTypesService service URI.</param>
        Public Sub New(ByVal serviceUri As Uri)
            Me.New(DomainContext.CreateDomainClient(GetType(IEFCoreComplexTypesServiceContract), serviceUri, false))
        End Sub
        
        ''' <summary>
        ''' Initializes a new instance of the <see cref="EFCoreComplexTypesContext"/> class with the specified <paramref name="domainClient"/>.
        ''' </summary>
        ''' <param name="domainClient">The DomainClient instance to use for this DomainContext.</param>
        Public Sub New(ByVal domainClient As DomainClient)
            MyBase.New(domainClient)
            Me.OnCreated
        End Sub
        
        ''' <summary>
        ''' Gets the set of <see cref="Employee"/> entity instances that have been loaded into this <see cref="EFCoreComplexTypesContext"/> instance.
        ''' </summary>
        Public ReadOnly Property Employees() As EntitySet(Of Employee)
            Get
                Return MyBase.EntityContainer.GetEntitySet(Of Employee)
            End Get
        End Property
        
        ''' <summary>
        ''' Gets an EntityQuery instance that can be used to load <see cref="Employee"/> entity instances using the 'GetCustomers' query.
        ''' </summary>
        ''' <returns>An EntityQuery that can be loaded to retrieve <see cref="Employee"/> entity instances.</returns>
        Public Function GetCustomersQuery() As EntityQuery(Of Employee)
            Me.ValidateMethod("GetCustomersQuery", Nothing)
            Return MyBase.CreateQuery(Of Employee)("GetCustomers", Nothing, false, true)
        End Function
        
        ''' <summary>
        ''' Creates a new EntityContainer for this DomainContext's EntitySets.
        ''' </summary>
        ''' <returns>A new container instance.</returns>
        Protected Overrides Function CreateEntityContainer() As EntityContainer
            Return New EFCoreComplexTypesContextEntityContainer()
        End Function
        
        ''' <summary>
        ''' Service contract for the 'EFCoreComplexTypesService' DomainService.
        ''' </summary>
        <ServiceContract()>  _
        Public Interface IEFCoreComplexTypesServiceContract
            
            ''' <summary>
            ''' Asynchronously invokes the 'GetCustomers' operation.
            ''' </summary>
            ''' <param name="callback">Callback to invoke on completion.</param>
            ''' <param name="asyncState">Optional state object.</param>
            ''' <returns>An IAsyncResult that can be used to monitor the request.</returns>
            <HasSideEffects(false),  _
             OperationContract(AsyncPattern:=true, Action:="http://tempuri.org/EFCoreComplexTypesService/GetCustomers", ReplyAction:="http://tempuri.org/EFCoreComplexTypesService/GetCustomersResponse")>  _
            Function BeginGetCustomers(ByVal callback As AsyncCallback, ByVal asyncState As Object) As IAsyncResult
            
            ''' <summary>
            ''' Completes the asynchronous operation begun by 'BeginGetCustomers'.
            ''' </summary>
            ''' <param name="result">The IAsyncResult returned from 'BeginGetCustomers'.</param>
            ''' <returns>The 'QueryResult' returned from the 'GetCustomers' operation.</returns>
            Function EndGetCustomers(ByVal result As IAsyncResult) As QueryResult(Of Employee)
        End Interface
        
        Friend NotInheritable Class EFCoreComplexTypesContextEntityContainer
            Inherits EntityContainer
            
            Public Sub New()
                MyBase.New
                Me.CreateEntitySet(Of Employee)(EntitySetOperations.None)
                Me.CreateEntitySet(Of OwnedEntityWithExplicitId)(EntitySetOperations.None)
                Me.CreateEntitySet(Of OwnedEntityWithExplicitIdAndBackNavigation)(EntitySetOperations.None)
            End Sub
        End Class
    End Class
End Namespace
