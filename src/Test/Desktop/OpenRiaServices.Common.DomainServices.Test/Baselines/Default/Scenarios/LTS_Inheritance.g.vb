'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.42000
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports OpenRiaServices
Imports OpenRiaServices.Client
Imports OpenRiaServices.Client.Authentication
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Linq
Imports System.Runtime.Serialization
Imports System.Threading
Imports System.Threading.Tasks

Namespace DataTests.Inheritance.LTS
    
    ''' <summary>
    ''' The 'A' entity class.
    ''' </summary>
    <DataContract([Namespace]:="http://schemas.datacontract.org/2004/07/DataTests.Inheritance.LTS"),  _
     KnownType(GetType(B)),  _
     KnownType(GetType(C))>  _
    Partial Public MustInherit Class A
        Inherits Entity
        
        Private _address As String
        
        Private _city As String
        
        Private _companyName As String
        
        Private _contactName As String
        
        Private _contactTitle As String
        
        Private _country As String
        
        Private _customerID As String
        
        Private _postalCode As String
        
        Private _region As String
        
        #Region "Extensibility Method Definitions"

        ''' <summary>
        ''' This method is invoked from the constructor once initialization is complete and
        ''' can be used for further object setup.
        ''' </summary>
        Private Partial Sub OnCreated()
        End Sub
        Private Partial Sub OnAddressChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnAddressChanged()
        End Sub
        Private Partial Sub OnCityChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnCityChanged()
        End Sub
        Private Partial Sub OnCompanyNameChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnCompanyNameChanged()
        End Sub
        Private Partial Sub OnContactNameChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnContactNameChanged()
        End Sub
        Private Partial Sub OnContactTitleChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnContactTitleChanged()
        End Sub
        Private Partial Sub OnCountryChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnCountryChanged()
        End Sub
        Private Partial Sub OnCustomerIDChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnCustomerIDChanged()
        End Sub
        Private Partial Sub OnPostalCodeChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnPostalCodeChanged()
        End Sub
        Private Partial Sub OnRegionChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnRegionChanged()
        End Sub

        #End Region
        
        
        ''' <summary>
        ''' Initializes a new instance of the <see cref="A"/> class.
        ''' </summary>
        Protected Sub New()
            MyBase.New
            Me.OnCreated
        End Sub
        
        ''' <summary>
        ''' Gets or sets the 'Address' value.
        ''' </summary>
        <ConcurrencyCheck(),  _
         DataMember(),  _
         RoundtripOriginal(),  _
         StringLength(60)>  _
        Public Property Address() As String
            Get
                Return Me._address
            End Get
            Set
                If (String.Equals(Me._address, value) = false) Then
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
        ''' Gets or sets the 'City' value.
        ''' </summary>
        <ConcurrencyCheck(),  _
         DataMember(),  _
         RoundtripOriginal(),  _
         StringLength(15)>  _
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
        
        ''' <summary>
        ''' Gets or sets the 'CompanyName' value.
        ''' </summary>
        <ConcurrencyCheck(),  _
         DataMember(),  _
         Required(),  _
         RoundtripOriginal(),  _
         StringLength(40)>  _
        Public Property CompanyName() As String
            Get
                Return Me._companyName
            End Get
            Set
                If (String.Equals(Me._companyName, value) = false) Then
                    Me.OnCompanyNameChanging(value)
                    Me.RaiseDataMemberChanging("CompanyName")
                    Me.ValidateProperty("CompanyName", value)
                    Me._companyName = value
                    Me.RaiseDataMemberChanged("CompanyName")
                    Me.OnCompanyNameChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the 'ContactName' value.
        ''' </summary>
        <ConcurrencyCheck(),  _
         DataMember(),  _
         RoundtripOriginal(),  _
         StringLength(30)>  _
        Public Property ContactName() As String
            Get
                Return Me._contactName
            End Get
            Set
                If (String.Equals(Me._contactName, value) = false) Then
                    Me.OnContactNameChanging(value)
                    Me.RaiseDataMemberChanging("ContactName")
                    Me.ValidateProperty("ContactName", value)
                    Me._contactName = value
                    Me.RaiseDataMemberChanged("ContactName")
                    Me.OnContactNameChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the 'ContactTitle' value.
        ''' </summary>
        <ConcurrencyCheck(),  _
         DataMember(),  _
         RoundtripOriginal(),  _
         StringLength(30)>  _
        Public Property ContactTitle() As String
            Get
                Return Me._contactTitle
            End Get
            Set
                If (String.Equals(Me._contactTitle, value) = false) Then
                    Me.OnContactTitleChanging(value)
                    Me.RaiseDataMemberChanging("ContactTitle")
                    Me.ValidateProperty("ContactTitle", value)
                    Me._contactTitle = value
                    Me.RaiseDataMemberChanged("ContactTitle")
                    Me.OnContactTitleChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the 'Country' value.
        ''' </summary>
        <ConcurrencyCheck(),  _
         DataMember(),  _
         RoundtripOriginal(),  _
         StringLength(15)>  _
        Public Property Country() As String
            Get
                Return Me._country
            End Get
            Set
                If (String.Equals(Me._country, value) = false) Then
                    Me.OnCountryChanging(value)
                    Me.RaiseDataMemberChanging("Country")
                    Me.ValidateProperty("Country", value)
                    Me._country = value
                    Me.RaiseDataMemberChanged("Country")
                    Me.OnCountryChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the 'CustomerID' value.
        ''' </summary>
        <ConcurrencyCheck(),  _
         DataMember(),  _
         Editable(false, AllowInitialValue:=true),  _
         Key(),  _
         Required(),  _
         RoundtripOriginal(),  _
         StringLength(5)>  _
        Public Property CustomerID() As String
            Get
                Return Me._customerID
            End Get
            Set
                If (String.Equals(Me._customerID, value) = false) Then
                    Me.OnCustomerIDChanging(value)
                    Me.ValidateProperty("CustomerID", value)
                    Me._customerID = value
                    Me.RaisePropertyChanged("CustomerID")
                    Me.OnCustomerIDChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the 'PostalCode' value.
        ''' </summary>
        <ConcurrencyCheck(),  _
         DataMember(),  _
         RoundtripOriginal(),  _
         StringLength(10)>  _
        Public Property PostalCode() As String
            Get
                Return Me._postalCode
            End Get
            Set
                If (String.Equals(Me._postalCode, value) = false) Then
                    Me.OnPostalCodeChanging(value)
                    Me.RaiseDataMemberChanging("PostalCode")
                    Me.ValidateProperty("PostalCode", value)
                    Me._postalCode = value
                    Me.RaiseDataMemberChanged("PostalCode")
                    Me.OnPostalCodeChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Gets or sets the 'Region' value.
        ''' </summary>
        <ConcurrencyCheck(),  _
         DataMember(),  _
         RoundtripOriginal(),  _
         StringLength(15)>  _
        Public Property Region() As String
            Get
                Return Me._region
            End Get
            Set
                If (String.Equals(Me._region, value) = false) Then
                    Me.OnRegionChanging(value)
                    Me.RaiseDataMemberChanging("Region")
                    Me.ValidateProperty("Region", value)
                    Me._region = value
                    Me.RaiseDataMemberChanged("Region")
                    Me.OnRegionChanged
                End If
            End Set
        End Property
        
        ''' <summary>
        ''' Computes a value from the key fields that uniquely identifies this entity instance.
        ''' </summary>
        ''' <returns>An object instance that uniquely identifies this entity instance.</returns>
        Public Overrides Function GetIdentity() As Object
            Return Me._customerID
        End Function
    End Class
    
    ''' <summary>
    ''' The 'B' entity class.
    ''' </summary>
    <DataContract([Namespace]:="http://schemas.datacontract.org/2004/07/DataTests.Inheritance.LTS")>  _
    Partial Public NotInheritable Class B
        Inherits A
        
        Private _phone As String
        
        #Region "Extensibility Method Definitions"

        ''' <summary>
        ''' This method is invoked from the constructor once initialization is complete and
        ''' can be used for further object setup.
        ''' </summary>
        Private Partial Sub OnCreated()
        End Sub
        Private Partial Sub OnPhoneChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnPhoneChanged()
        End Sub

        #End Region
        
        
        ''' <summary>
        ''' Initializes a new instance of the <see cref="B"/> class.
        ''' </summary>
        Public Sub New()
            MyBase.New
            Me.OnCreated
        End Sub
        
        ''' <summary>
        ''' Gets or sets the 'Phone' value.
        ''' </summary>
        <ConcurrencyCheck(),  _
         DataMember(),  _
         RoundtripOriginal(),  _
         StringLength(24)>  _
        Public Property Phone() As String
            Get
                Return Me._phone
            End Get
            Set
                If (String.Equals(Me._phone, value) = false) Then
                    Me.OnPhoneChanging(value)
                    Me.RaiseDataMemberChanging("Phone")
                    Me.ValidateProperty("Phone", value)
                    Me._phone = value
                    Me.RaiseDataMemberChanged("Phone")
                    Me.OnPhoneChanged
                End If
            End Set
        End Property
    End Class
    
    ''' <summary>
    ''' The 'C' entity class.
    ''' </summary>
    <DataContract([Namespace]:="http://schemas.datacontract.org/2004/07/DataTests.Inheritance.LTS")>  _
    Partial Public NotInheritable Class C
        Inherits A
        
        Private _fax As String
        
        #Region "Extensibility Method Definitions"

        ''' <summary>
        ''' This method is invoked from the constructor once initialization is complete and
        ''' can be used for further object setup.
        ''' </summary>
        Private Partial Sub OnCreated()
        End Sub
        Private Partial Sub OnFaxChanging(ByVal value As String)
        End Sub
        Private Partial Sub OnFaxChanged()
        End Sub

        #End Region
        
        
        ''' <summary>
        ''' Initializes a new instance of the <see cref="C"/> class.
        ''' </summary>
        Public Sub New()
            MyBase.New
            Me.OnCreated
        End Sub
        
        ''' <summary>
        ''' Gets or sets the 'Fax' value.
        ''' </summary>
        <ConcurrencyCheck(),  _
         DataMember(),  _
         RoundtripOriginal(),  _
         StringLength(24)>  _
        Public Property Fax() As String
            Get
                Return Me._fax
            End Get
            Set
                If (String.Equals(Me._fax, value) = false) Then
                    Me.OnFaxChanging(value)
                    Me.RaiseDataMemberChanging("Fax")
                    Me.ValidateProperty("Fax", value)
                    Me._fax = value
                    Me.RaiseDataMemberChanged("Fax")
                    Me.OnFaxChanged
                End If
            End Set
        End Property
    End Class
    
    ''' <summary>
    ''' The DomainContext corresponding to the 'LTS_Inheritance_DomainService' DomainService.
    ''' </summary>
    Partial Public NotInheritable Class LTS_Inheritance_DomainContext
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
        ''' Initializes a new instance of the <see cref="LTS_Inheritance_DomainContext"/> class.
        ''' </summary>
        Public Sub New()
            Me.New(New Uri("DataTests-Inheritance-LTS-LTS_Inheritance_DomainService.svc", UriKind.Relative))
        End Sub
        
        ''' <summary>
        ''' Initializes a new instance of the <see cref="LTS_Inheritance_DomainContext"/> class with the specified service URI.
        ''' </summary>
        ''' <param name="serviceUri">The LTS_Inheritance_DomainService service URI.</param>
        Public Sub New(ByVal serviceUri As Uri)
            Me.New(DomainContext.CreateDomainClient(GetType(ILTS_Inheritance_DomainServiceContract), serviceUri, false))
        End Sub
        
        ''' <summary>
        ''' Initializes a new instance of the <see cref="LTS_Inheritance_DomainContext"/> class with the specified <paramref name="domainClient"/>.
        ''' </summary>
        ''' <param name="domainClient">The DomainClient instance to use for this DomainContext.</param>
        Public Sub New(ByVal domainClient As DomainClient)
            MyBase.New(domainClient)
            Me.OnCreated
        End Sub
        
        ''' <summary>
        ''' Gets the set of <see cref="A"/> entity instances that have been loaded into this <see cref="LTS_Inheritance_DomainContext"/> instance.
        ''' </summary>
        Public ReadOnly Property [As]() As EntitySet(Of A)
            Get
                Return MyBase.EntityContainer.GetEntitySet(Of A)
            End Get
        End Property
        
        ''' <summary>
        ''' Gets an EntityQuery instance that can be used to load <see cref="A"/> entity instances using the 'GetA' query.
        ''' </summary>
        ''' <returns>An EntityQuery that can be loaded to retrieve <see cref="A"/> entity instances.</returns>
        Public Function GetAQuery() As EntityQuery(Of A)
            Me.ValidateMethod("GetAQuery", Nothing)
            Return MyBase.CreateQuery(Of A)("GetA", Nothing, false, true)
        End Function
        
        ''' <summary>
        ''' Gets an EntityQuery instance that can be used to load <see cref="B"/> entity instances using the 'GetB' query.
        ''' </summary>
        ''' <returns>An EntityQuery that can be loaded to retrieve <see cref="B"/> entity instances.</returns>
        Public Function GetBQuery() As EntityQuery(Of B)
            Me.ValidateMethod("GetBQuery", Nothing)
            Return MyBase.CreateQuery(Of B)("GetB", Nothing, false, true)
        End Function
        
        ''' <summary>
        ''' Gets an EntityQuery instance that can be used to load <see cref="C"/> entity instances using the 'GetC' query.
        ''' </summary>
        ''' <returns>An EntityQuery that can be loaded to retrieve <see cref="C"/> entity instances.</returns>
        Public Function GetCQuery() As EntityQuery(Of C)
            Me.ValidateMethod("GetCQuery", Nothing)
            Return MyBase.CreateQuery(Of C)("GetC", Nothing, false, true)
        End Function
        
        ''' <summary>
        ''' Gets an EntityQuery instance that can be used to load <see cref="B"/> entity instances using the 'GetOneB' query.
        ''' </summary>
        ''' <returns>An EntityQuery that can be loaded to retrieve <see cref="B"/> entity instances.</returns>
        Public Function GetOneBQuery() As EntityQuery(Of B)
            Me.ValidateMethod("GetOneBQuery", Nothing)
            Return MyBase.CreateQuery(Of B)("GetOneB", Nothing, false, false)
        End Function
        
        ''' <summary>
        ''' Gets an EntityQuery instance that can be used to load <see cref="C"/> entity instances using the 'GetOneC' query.
        ''' </summary>
        ''' <returns>An EntityQuery that can be loaded to retrieve <see cref="C"/> entity instances.</returns>
        Public Function GetOneCQuery() As EntityQuery(Of C)
            Me.ValidateMethod("GetOneCQuery", Nothing)
            Return MyBase.CreateQuery(Of C)("GetOneC", Nothing, false, false)
        End Function
        
        ''' <summary>
        ''' Asynchronously invokes the 'InvokeOnB' method of the DomainService.
        ''' </summary>
        ''' <param name="b">The value for the 'b' parameter of this action.</param>
        ''' <param name="callback">Callback to invoke when the operation completes.</param>
        ''' <param name="userState">Value to pass to the callback.  It can be <c>null</c>.</param>
        ''' <returns>An operation instance that can be used to manage the asynchronous request.</returns>
        Public Overloads Function InvokeOnB(ByVal b As B, ByVal callback As Action(Of InvokeOperation(Of Integer)), ByVal userState As Object) As InvokeOperation(Of Integer)
            Dim parameters As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
            parameters.Add("b", b)
            Me.ValidateMethod("InvokeOnB", parameters)
            Return Me.InvokeOperation(Of Integer)("InvokeOnB", GetType(Integer), parameters, true, callback, userState)
        End Function
        
        ''' <summary>
        ''' Asynchronously invokes the 'InvokeOnB' method of the DomainService.
        ''' </summary>
        ''' <param name="b">The value for the 'b' parameter of this action.</param>
        ''' <returns>An operation instance that can be used to manage the asynchronous request.</returns>
        Public Overloads Function InvokeOnB(ByVal b As B) As InvokeOperation(Of Integer)
            Dim parameters As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
            parameters.Add("b", b)
            Me.ValidateMethod("InvokeOnB", parameters)
            Return Me.InvokeOperation(Of Integer)("InvokeOnB", GetType(Integer), parameters, true, Nothing, Nothing)
        End Function
        
        ''' <summary>
        ''' Asynchronously invokes the 'InvokeOnB' method of the DomainService.
        ''' </summary>
        ''' <param name="b">The value for the 'b' parameter of this action.</param>
        ''' <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        ''' <returns>An operation instance that can be used to manage the asynchronous request.</returns>
        Public Function InvokeOnBAsync(ByVal b As B, Optional ByVal cancellationToken As CancellationToken = Nothing) As System.Threading.Tasks.Task(Of InvokeResult(Of Integer))
            Dim parameters As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
            parameters.Add("b", b)
            Me.ValidateMethod("InvokeOnB", parameters)
            Return Me.InvokeOperationAsync(Of Integer)("InvokeOnB", parameters, true, cancellationToken)
        End Function
        
        ''' <summary>
        ''' Creates a new EntityContainer for this DomainContext's EntitySets.
        ''' </summary>
        ''' <returns>A new container instance.</returns>
        Protected Overrides Function CreateEntityContainer() As EntityContainer
            Return New LTS_Inheritance_DomainContextEntityContainer()
        End Function
        
        ''' <summary>
        ''' Service contract for the 'LTS_Inheritance_DomainService' DomainService.
        ''' </summary>
        Public Interface ILTS_Inheritance_DomainServiceContract
            
            ''' <summary>
            ''' Asynchronously invokes the 'GetA' operation.
            ''' </summary>
            ''' <param name="callback">Callback to invoke on completion.</param>
            ''' <param name="asyncState">Optional state object.</param>
            ''' <returns>An IAsyncResult that can be used to monitor the request.</returns>
            <HasSideEffects(false)>  _
            Function BeginGetA(ByVal callback As AsyncCallback, ByVal asyncState As Object) As IAsyncResult
            
            ''' <summary>
            ''' Completes the asynchronous operation begun by 'BeginGetA'.
            ''' </summary>
            ''' <param name="result">The IAsyncResult returned from 'BeginGetA'.</param>
            ''' <returns>The 'QueryResult' returned from the 'GetA' operation.</returns>
            Function EndGetA(ByVal result As IAsyncResult) As QueryResult(Of A)
            
            ''' <summary>
            ''' Asynchronously invokes the 'GetB' operation.
            ''' </summary>
            ''' <param name="callback">Callback to invoke on completion.</param>
            ''' <param name="asyncState">Optional state object.</param>
            ''' <returns>An IAsyncResult that can be used to monitor the request.</returns>
            <HasSideEffects(false)>  _
            Function BeginGetB(ByVal callback As AsyncCallback, ByVal asyncState As Object) As IAsyncResult
            
            ''' <summary>
            ''' Completes the asynchronous operation begun by 'BeginGetB'.
            ''' </summary>
            ''' <param name="result">The IAsyncResult returned from 'BeginGetB'.</param>
            ''' <returns>The 'QueryResult' returned from the 'GetB' operation.</returns>
            Function EndGetB(ByVal result As IAsyncResult) As QueryResult(Of B)
            
            ''' <summary>
            ''' Asynchronously invokes the 'GetC' operation.
            ''' </summary>
            ''' <param name="callback">Callback to invoke on completion.</param>
            ''' <param name="asyncState">Optional state object.</param>
            ''' <returns>An IAsyncResult that can be used to monitor the request.</returns>
            <HasSideEffects(false)>  _
            Function BeginGetC(ByVal callback As AsyncCallback, ByVal asyncState As Object) As IAsyncResult
            
            ''' <summary>
            ''' Completes the asynchronous operation begun by 'BeginGetC'.
            ''' </summary>
            ''' <param name="result">The IAsyncResult returned from 'BeginGetC'.</param>
            ''' <returns>The 'QueryResult' returned from the 'GetC' operation.</returns>
            Function EndGetC(ByVal result As IAsyncResult) As QueryResult(Of C)
            
            ''' <summary>
            ''' Asynchronously invokes the 'GetOneB' operation.
            ''' </summary>
            ''' <param name="callback">Callback to invoke on completion.</param>
            ''' <param name="asyncState">Optional state object.</param>
            ''' <returns>An IAsyncResult that can be used to monitor the request.</returns>
            <HasSideEffects(false)>  _
            Function BeginGetOneB(ByVal callback As AsyncCallback, ByVal asyncState As Object) As IAsyncResult
            
            ''' <summary>
            ''' Completes the asynchronous operation begun by 'BeginGetOneB'.
            ''' </summary>
            ''' <param name="result">The IAsyncResult returned from 'BeginGetOneB'.</param>
            ''' <returns>The 'QueryResult' returned from the 'GetOneB' operation.</returns>
            Function EndGetOneB(ByVal result As IAsyncResult) As QueryResult(Of B)
            
            ''' <summary>
            ''' Asynchronously invokes the 'GetOneC' operation.
            ''' </summary>
            ''' <param name="callback">Callback to invoke on completion.</param>
            ''' <param name="asyncState">Optional state object.</param>
            ''' <returns>An IAsyncResult that can be used to monitor the request.</returns>
            <HasSideEffects(false)>  _
            Function BeginGetOneC(ByVal callback As AsyncCallback, ByVal asyncState As Object) As IAsyncResult
            
            ''' <summary>
            ''' Completes the asynchronous operation begun by 'BeginGetOneC'.
            ''' </summary>
            ''' <param name="result">The IAsyncResult returned from 'BeginGetOneC'.</param>
            ''' <returns>The 'QueryResult' returned from the 'GetOneC' operation.</returns>
            Function EndGetOneC(ByVal result As IAsyncResult) As QueryResult(Of C)
            
            ''' <summary>
            ''' Asynchronously invokes the 'InvokeOnB' operation.
            ''' </summary>
            ''' <param name="b">The value for the 'b' parameter of this action.</param>
            ''' <param name="callback">Callback to invoke on completion.</param>
            ''' <param name="asyncState">Optional state object.</param>
            ''' <returns>An IAsyncResult that can be used to monitor the request.</returns>
            <HasSideEffects(true)>  _
            Function BeginInvokeOnB(ByVal b As B, ByVal callback As AsyncCallback, ByVal asyncState As Object) As IAsyncResult
            
            ''' <summary>
            ''' Completes the asynchronous operation begun by 'BeginInvokeOnB'.
            ''' </summary>
            ''' <param name="result">The IAsyncResult returned from 'BeginInvokeOnB'.</param>
            ''' <returns>The 'Int32' returned from the 'InvokeOnB' operation.</returns>
            Function EndInvokeOnB(ByVal result As IAsyncResult) As Integer
            
            ''' <summary>
            ''' Asynchronously invokes the 'SubmitChanges' operation.
            ''' </summary>
            ''' <param name="changeSet">The change-set to submit.</param>
            ''' <param name="callback">Callback to invoke on completion.</param>
            ''' <param name="asyncState">Optional state object.</param>
            ''' <returns>An IAsyncResult that can be used to monitor the request.</returns>
            Function BeginSubmitChanges(ByVal changeSet As IEnumerable(Of ChangeSetEntry), ByVal callback As AsyncCallback, ByVal asyncState As Object) As IAsyncResult
            
            ''' <summary>
            ''' Completes the asynchronous operation begun by 'BeginSubmitChanges'.
            ''' </summary>
            ''' <param name="result">The IAsyncResult returned from 'BeginSubmitChanges'.</param>
            ''' <returns>The collection of change-set entry elements returned from 'SubmitChanges'.</returns>
            Function EndSubmitChanges(ByVal result As IAsyncResult) As IEnumerable(Of ChangeSetEntry)
        End Interface
        
        Friend NotInheritable Class LTS_Inheritance_DomainContextEntityContainer
            Inherits EntityContainer
            
            Public Sub New()
                MyBase.New
                Me.CreateEntitySet(Of A)(EntitySetOperations.Edit)
            End Sub
        End Class
    End Class
End Namespace
