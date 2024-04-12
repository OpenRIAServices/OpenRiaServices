﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version: 17.0.0.0
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------
namespace OpenRiaServices.Tools.TextTemplate.CSharpGenerators
{
    using System;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Linq;
    using OpenRiaServices.Server;
    using OpenRiaServices.Tools.TextTemplate;
    using OpenRiaServices.Tools;
    using System.Runtime.Serialization;
    using System.Reflection;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public partial class CSharpEntityGenerator : OpenRiaServices.Tools.TextTemplate.EntityGenerator
    {
        /// <summary>
        /// Create the template output
        /// </summary>
        public override string TransformText()
        {
            this.Write("\r\n");
            this.Write("\r\n");
            this.Write("\r\n");
            this.Write("\r\n");
            this.Write("\r\n");
 this.Generate(); 
            this.Write("\r\n");
            return this.GenerationEnvironment.ToString();
        }
	
	private void GenerateCustomMethod(DomainOperationEntry customMethod)
	{
		string methodInvokingName = "On" + customMethod.Name + "Invoking";
        string methodInvokedName = "On" + customMethod.Name + "Invoked";		
		List<KeyValuePair<string, string>> customMethodParameters = new List<KeyValuePair<string, string>>();
		List<DomainOperationParameter> domainOperationparameterList = new List<DomainOperationParameter>();
		for(int i = 1; i < customMethod.Parameters.Count(); i++)
		{
			DomainOperationParameter paramInfo = customMethod.Parameters[i];
			customMethodParameters.Add(new KeyValuePair<string, string>(CodeGenUtilities.GetTypeName(CodeGenUtilities.TranslateType(paramInfo.ParameterType)), paramInfo.Name));
			domainOperationparameterList.Add(paramInfo);
		}

		var customMethodAttribute = customMethod.OperationAttribute as EntityActionAttribute;
		bool allowMultipleInvocations = customMethodAttribute != null && customMethodAttribute.AllowMultipleInvocations;

this.Write("[OpenRiaServices.Client.EntityAction(\"");

this.Write(this.ToStringHelper.ToStringWithCulture(customMethod.Name));

this.Write("\", AllowMultipleInvocations = ");

this.Write(this.ToStringHelper.ToStringWithCulture(allowMultipleInvocations.ToString().ToLower()));

this.Write(")]\r\npublic void ");

this.Write(this.ToStringHelper.ToStringWithCulture(customMethod.Name));

this.Write("(");


this.GenerateParameterDeclaration(domainOperationparameterList, true);

this.Write(")\r\n");

		
		this.GenerateOpeningBrace();

this.Write("this.");

this.Write(this.ToStringHelper.ToStringWithCulture(methodInvokingName));

this.Write("(");

 this.GenerateParametersForMethodCall(domainOperationparameterList); 
this.Write(");\r\nbase.InvokeAction(\"");

this.Write(this.ToStringHelper.ToStringWithCulture(customMethod.Name));

this.Write("\"");

 if(domainOperationparameterList.Count > 0){
this.Write(", ");

 this.GenerateParametersForMethodCall(domainOperationparameterList); }
this.Write(");\r\nthis.");

this.Write(this.ToStringHelper.ToStringWithCulture(methodInvokedName));

this.Write("();\r\n");

		
		this.GenerateClosingBrace();

this.Write("partial void ");

this.Write(this.ToStringHelper.ToStringWithCulture(methodInvokingName));

this.Write("(");

 this.GenerateParameterDeclaration(domainOperationparameterList, false); 
this.Write(");\r\npartial void ");

this.Write(this.ToStringHelper.ToStringWithCulture(methodInvokedName));

this.Write("();\r\n");


	}
	
	private void GenerateParametersForMethodCall(IEnumerable<DomainOperationParameter> parameters)
	{
		DomainOperationParameter[] paramArr = parameters.ToArray();
		for(int i = 0; i < paramArr.Length; i++)
		{
			
this.Write(this.ToStringHelper.ToStringWithCulture(paramArr[i].Name));


			if(i + 1 < paramArr.Length)
			{
				
this.Write(", ");


			}
		}
	}
	
	private void GenerateCustomMethodProperties(DomainOperationEntry customMethod)
	{

this.Write("[System.ComponentModel.DataAnnotations.Display(AutoGenerateField=false)]\r\npublic " +
        "bool Can");

this.Write(this.ToStringHelper.ToStringWithCulture(customMethod.Name));

this.Write("\r\n{\r\n    get\r\n    {\r\n        return base.CanInvokeAction(\"");

this.Write(this.ToStringHelper.ToStringWithCulture(customMethod.Name));

this.Write("\");\r\n    }\r\n}\r\n\r\n[System.ComponentModel.DataAnnotations.Display(AutoGenerateField" +
        "=false)]\r\npublic bool Is");

this.Write(this.ToStringHelper.ToStringWithCulture(customMethod.Name));

this.Write("Invoked\r\n{\r\n\tget\r\n\t{\r\n\t\treturn base.IsActionInvoked(\"");

this.Write(this.ToStringHelper.ToStringWithCulture(customMethod.Name));

this.Write("\");\r\n\t}\r\n}\r\n");


	}

	/// <summary>
	/// Generates the GetIdentity() method on the entity.
	/// </summary>
	protected virtual void GenerateGetIdentityMethod()
	{
		string[] keyNames;
		string[] nullableKeyNames;
		this.GetKeysInfo(out keyNames, out nullableKeyNames);
		if(keyNames != null && keyNames.Count() > 0)
		{

this.Write("public override object GetIdentity()\r\n");

  
			this.GenerateOpeningBrace();
			if(keyNames.Count() == 1)
			{

this.Write("return this.");

this.Write(this.ToStringHelper.ToStringWithCulture(keyNames[0]));

this.Write(";\r\n");


			}
			else
			{
				if(nullableKeyNames.Count() > 0)
				{
					
this.Write("if(");

 
						for(int i = 0; i < nullableKeyNames.Count(); i++)
						{
							
this.Write("this.");

this.Write(this.ToStringHelper.ToStringWithCulture(nullableKeyNames[i]));

this.Write(" == null");


							if(i + 1 < nullableKeyNames.Count())
							{
								
this.Write(" || ");


							}
						}	

this.Write(")\r\n{\r\n\treturn null;\r\n}\r\n");


				}

this.Write("return OpenRiaServices.Client.EntityKey.Create(");


for(int i = 0; i < keyNames.Count(); i++)
{
	
this.Write("this.");

this.Write(this.ToStringHelper.ToStringWithCulture(keyNames[i]));


	if(i + 1 < keyNames.Count())
	{
		
this.Write(", ");


	}
}	

this.Write(");\r\n");


				
			}
			this.GenerateClosingBrace();
		}
	}
	
	private void GenerateAdditionalUserCode()
	{
		// Generate Properties / methods for IPrincipal and IIdentity interfaces. We are guaranteed to have properties called Name and Roles.
		// We simply generate code as it is, since it is not dependent on anything.
		

this.Write(@"string global::System.Security.Principal.IIdentity.AuthenticationType
{
    get
    {
        return string.Empty;
    }
}

public bool IsAuthenticated
{
    get
    {
        return (true != string.IsNullOrEmpty(this.Name));
    }
}

string global::System.Security.Principal.IIdentity.Name
{
    get
    {
        return this.Name;
    }
}

global::System.Security.Principal.IIdentity global::System.Security.Principal.IPrincipal.Identity
{
    get
    {
        return this;
    }
}

public bool IsInRole(string role)
{
    if ((this.Roles == null))
    {
        return false;
    }
    return global::System.Linq.Enumerable.Contains(this.Roles, role);
}
");


	}



	private void GenerateNamespace()
	{

this.Write("namespace ");

this.Write(this.ToStringHelper.ToStringWithCulture(this.Type.Namespace));

this.Write("\r\n");


	}
	
	/// <summary>
    /// Generates the type declaration.
    /// </summary>
	protected virtual void GenerateClassDeclaration()
	{
		this.GenerateTypeAttributes();
		
		string baseType = this.GetBaseTypeName();
		string visibility = this.GetClassVisibility();

this.Write(this.ToStringHelper.ToStringWithCulture(visibility));

this.Write(" partial class ");

this.Write(this.ToStringHelper.ToStringWithCulture(CodeGenUtilities.GetSafeName(this.Type.Name)));

this.Write(" : ");

this.Write(this.ToStringHelper.ToStringWithCulture(baseType));

this.Write("\r\n");


	}
	
	private string GetClassVisibility()
    {
        string visibility = "public";
        if (this.IsAbstract)
        {
            visibility += " abstract";
        }
        if (!this.IsAbstract && !this.GetDerivedTypes().Any())
        {
            visibility += " sealed";
        }
        return visibility;
    }
	
	private void GenerateTypeAttributes()
	{
		IEnumerable<Attribute> typeAttributes = this.GetTypeAttributes();
		this.GenerateAttributes(typeAttributes);
		this.GenerateDataContractAttribute(this.Type);
		
		if(!this.IsDerivedType)
		{
			foreach (Type derivedType in this.GetDerivedTypes().OrderBy(t => t.FullName))
            {

this.Write("[System.Runtime.Serialization.KnownType(typeof(");

this.Write(this.ToStringHelper.ToStringWithCulture(CodeGenUtilities.GetTypeName(derivedType)));

this.Write("))]\r\n");


			}
		}
	}
	
	/// <summary>
    /// Generates the data contract type constructor.
    /// </summary>
	protected virtual void GenerateConstructor()
	{
		string ctorVisibility = this.IsAbstract ? "protected" : "public";

this.Write(this.ToStringHelper.ToStringWithCulture(ctorVisibility));

this.Write(" ");

this.Write(this.ToStringHelper.ToStringWithCulture(CodeGenUtilities.GetSafeName(this.Type.Name)));

this.Write("()\r\n{\r\n\tthis.OnCreated();\r\n}\r\n");


	}
	
	private void GeneratePropertiesInternal()
	{
		foreach(var property in this.Properties)
		{
			this.GenerateProperty(property);
		}
	}
	
	/// <summary>
    /// Generates the code for a property.
	/// <param name="propertyDescriptor">The PropertyDescriptor for which the property is to be generated.</param>
    /// </summary>
	protected virtual void GenerateProperty(PropertyDescriptor propertyDescriptor)
	{
		this.GeneratePropertyDeclaration(propertyDescriptor);
		this.GenerateOpeningBrace();
		this.GeneratePropertyGetter(propertyDescriptor);
		this.GeneratePropertySetter(propertyDescriptor);
		this.GenerateClosingBrace();
		this.GenerateBackingPrivateField(propertyDescriptor);
	}
	
	private void GeneratePropertyGetter(PropertyDescriptor propertyDescriptor)
	{
		string fieldName = CodeGenUtilities.MakeCompliantFieldName(propertyDescriptor.Name);

this.Write("get\r\n{\r\n\treturn this.");

this.Write(this.ToStringHelper.ToStringWithCulture(fieldName));

this.Write(";\r\n} \r\n");


	}
	
	private void GeneratePropertySetter(PropertyDescriptor propertyDescriptor)
	{
		string fieldName = CodeGenUtilities.MakeCompliantFieldName(propertyDescriptor.Name);

this.Write("set \r\n");

 this.GenerateOpeningBrace(); 

this.Write("if(this.");

this.Write(this.ToStringHelper.ToStringWithCulture(fieldName));

this.Write(" != value)\r\n");

 this.GenerateOpeningBrace(); 

this.Write("this.On");

this.Write(this.ToStringHelper.ToStringWithCulture(propertyDescriptor.Name));

this.Write("Changing(value);\t\r\n");


		bool propertyIsReadOnly = this.IsPropertyReadOnly(propertyDescriptor);
        if (!propertyIsReadOnly)
        {

this.Write("this.RaiseDataMemberChanging(\"");

this.Write(this.ToStringHelper.ToStringWithCulture(propertyDescriptor.Name));

this.Write("\");\r\n");


		}

this.Write("this.ValidateProperty(\"");

this.Write(this.ToStringHelper.ToStringWithCulture(propertyDescriptor.Name));

this.Write("\", value);\r\nthis.");

this.Write(this.ToStringHelper.ToStringWithCulture(fieldName));

this.Write(" = value;\r\n");


		if (!propertyIsReadOnly)
        {

this.Write("this.RaiseDataMemberChanged(\"");

this.Write(this.ToStringHelper.ToStringWithCulture(propertyDescriptor.Name));

this.Write("\");\r\n");


		}
		else
		{

this.Write("this.RaisePropertyChanged(\"");

this.Write(this.ToStringHelper.ToStringWithCulture(propertyDescriptor.Name));

this.Write("\");\r\n");


		}	

this.Write("this.On");

this.Write(this.ToStringHelper.ToStringWithCulture(propertyDescriptor.Name));

this.Write("Changed();\r\n");


		this.GenerateClosingBrace();
		this.GenerateClosingBrace();
	}
	
	private void GenerateBackingPrivateField(PropertyDescriptor propertyDescriptor)
	{
		Type propertyType = CodeGenUtilities.TranslateType(propertyDescriptor.PropertyType);
		string propertyTypeName = CodeGenUtilities.GetTypeName(propertyType);
		string fieldName = CodeGenUtilities.MakeCompliantFieldName(propertyDescriptor.Name);

this.Write("private ");

this.Write(this.ToStringHelper.ToStringWithCulture(propertyTypeName));

this.Write(" ");

this.Write(this.ToStringHelper.ToStringWithCulture(fieldName));

this.Write(";\r\n");


	}
	
	private void GeneratePropertyDeclaration(PropertyDescriptor propertyDescriptor)
	{		
		Type propertyType = CodeGenUtilities.TranslateType(propertyDescriptor.PropertyType);
		string propertyTypeName = CodeGenUtilities.GetTypeName(propertyType);
		IEnumerable<Attribute> propAttributes = this.GetPropertyAttributes(propertyDescriptor, propertyType);
		string propertyName = CodeGenUtilities.GetSafeName(propertyDescriptor.Name);
		this.GenerateAttributes(propAttributes);

this.Write("public ");

this.Write(this.ToStringHelper.ToStringWithCulture(propertyTypeName));

this.Write(" ");

this.Write(this.ToStringHelper.ToStringWithCulture(propertyName));

this.Write("\r\n");


	}

	/// <summary>
	/// Generates the notification methods on the class.
	/// </summary>
	protected virtual void GenerateNotificationMethods()
	{

this.Write("partial void OnCreated();\r\n");


		foreach(PropertyDescriptor pd in this.NotificationMethodList)
		{
			Type propType = CodeGenUtilities.TranslateType(pd.PropertyType);			
			string propertyTypeName = CodeGenUtilities.GetTypeName(propType);

this.Write("partial void On");

this.Write(this.ToStringHelper.ToStringWithCulture(pd.Name));

this.Write("Changing(");

this.Write(this.ToStringHelper.ToStringWithCulture(propertyTypeName));

this.Write(" value);\r\npartial void On");

this.Write(this.ToStringHelper.ToStringWithCulture(pd.Name));

this.Write("Changed();\r\n");


		}
	}



private void GenerateParameterDeclaration(IEnumerable<DomainOperationParameter> parameters, bool generateAttributes)
{
	DomainOperationParameter[] paramInfos = parameters.ToArray();
	for(int i = 0; i < paramInfos.Length; i++)
	{
		DomainOperationParameter paramInfo = paramInfos[i];
		if(generateAttributes)
		{
			IEnumerable<Attribute> paramAttributes = paramInfo.Attributes.Cast<Attribute>();
			this.GenerateAttributes(paramAttributes);
		}
		string paramTypeName = CodeGenUtilities.GetTypeName(CodeGenUtilities.TranslateType(paramInfo.ParameterType));
		string paramName = CodeGenUtilities.GetSafeName(paramInfo.Name);
		
this.Write(this.ToStringHelper.ToStringWithCulture(paramTypeName));

this.Write(" ");

this.Write(this.ToStringHelper.ToStringWithCulture(paramName));


		if(i + 1 < paramInfos.Length)
		{
			
this.Write(", ");


		}
	}
}

private void GenerateOpeningBrace()
{

this.Write("{\r\n");


	PushIndent("\t");
}

private void GenerateClosingBrace()
{
	PopIndent();

this.Write("}\r\n");


}

private void GenerateNamespace(string ns)
{

this.Write("namespace ");

this.Write(this.ToStringHelper.ToStringWithCulture(ns));

this.Write("\r\n");


}



	/// <summary>
	/// Generates attribute declarations in C#.
	/// </summary>
	/// <param name="attributes">The list of attributes to be generated.</param>
	protected virtual void GenerateAttributes(IEnumerable<Attribute> attributes)
	{	
		this.GenerateAttributes(attributes, false);
	}
	
	/// <summary>
	/// Generates attribute declarations in C#.
	/// </summary>
	/// <param name="attributes">The attributes to be generated.</param>
	/// <param name="forcePropagation">Causes the attributes to be generated even if the attribute verification fails.</param>
	protected virtual void GenerateAttributes(IEnumerable<Attribute> attributes, bool forcePropagation)
	{
		foreach (Attribute attribute in attributes)
        {
			AttributeDeclaration attributeDeclaration = AttributeGeneratorHelper.GetAttributeDeclaration(attribute, this.ClientCodeGenerator, forcePropagation);
            if (attributeDeclaration == null || attributeDeclaration.HasErrors)
			{
				continue;
			}
			
			string attributeTypeName = CodeGenUtilities.GetTypeName(attributeDeclaration.AttributeType);

this.Write("[");

this.Write(this.ToStringHelper.ToStringWithCulture(attributeTypeName));

this.Write("(");


			if (attributeDeclaration.ConstructorArguments.Count > 0)
            {
				for (int i = 0; i < attributeDeclaration.ConstructorArguments.Count; i++)
            	{
                	object value = attributeDeclaration.ConstructorArguments[i];
					string stringValue = AttributeGeneratorHelper.ConvertValueToCode(value, true);
					
this.Write(this.ToStringHelper.ToStringWithCulture(stringValue));


					if (i + 1 < attributeDeclaration.ConstructorArguments.Count)
					{
					
this.Write(", ");


					}
	            }
			}
			if (attributeDeclaration.NamedParameters.Count > 0)
            {
				if (attributeDeclaration.ConstructorArguments.Count > 0)
            	{
					
this.Write(", ");


				}
				
				for (int i = 0; i < attributeDeclaration.NamedParameters.Count; i++)
                {
                    KeyValuePair<string, object> pair = attributeDeclaration.NamedParameters.ElementAt(i);
                    string stringValue = AttributeGeneratorHelper.ConvertValueToCode(pair.Value, true);
					
this.Write(this.ToStringHelper.ToStringWithCulture(pair.Key));

this.Write("=");

this.Write(this.ToStringHelper.ToStringWithCulture(stringValue));


                    if (i + 1 < attributeDeclaration.NamedParameters.Count)
                    {
					
this.Write(",");


                    }
                }
			}

this.Write(")]\r\n");


		}
	}
	
	private void GenerateDataContractAttribute(Type sourceType)
	{
		string dataContractNamespace, dataContractName;
		AttributeGeneratorHelper.GetContractNameAndNamespace(sourceType, out dataContractNamespace, out dataContractName);

this.Write("[System.Runtime.Serialization.DataContract(Namespace = \"");

this.Write(this.ToStringHelper.ToStringWithCulture(dataContractNamespace));

this.Write("\"");

  
		if(!string.IsNullOrEmpty(dataContractName))
		{
		
this.Write(", Name = \" ");

this.Write(this.ToStringHelper.ToStringWithCulture(dataContractName));

this.Write("\"");


		}

this.Write(")]\r\n");


	}	


    private void GenerateSingletonAssociation(PropertyDescriptor pd)
    {
		AssociationMetadata metadata = new AssociationMetadata(pd);
		this.GenerateAssociationField(metadata);
		this.GenerateSingletonAssociationProperty(metadata);
		this.GenerateAssociationFilterMethod(metadata);
    }

	private void GenerateAssociationField(AssociationMetadata metadata)
	{		

this.Write("private ");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.AssociationTypeName));

this.Write(" ");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.FieldName));

this.Write(";\r\n");


	}
	
	private void GenerateSingletonAssociationProperty(AssociationMetadata metadata)
	{
		this.GenerateAssociationPropertyDeclaration(metadata);
		this.GenerateOpeningBrace();
		this.GenerateSingletonAssociationPropertyGetter(metadata);
		this.GenerateSingletonAssociationPropertySetter(metadata);
		this.GenerateClosingBrace();
	}
	
	private void GenerateAssociationPropertyDeclaration(AssociationMetadata metadata)
	{
		this.GenerateAttributes(metadata.Attributes);
		string propertyType = String.Empty;
		if(metadata.IsCollection)
		{
			propertyType = metadata.AssociationTypeName;			
		}
		else
		{
			propertyType = metadata.PropTypeName;
		}

this.Write("public ");

this.Write(this.ToStringHelper.ToStringWithCulture(propertyType));

this.Write(" ");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.PropertyName));

this.Write("\r\n");


	}
	
	private void GenerateSingletonAssociationPropertyGetter(AssociationMetadata metadata)
	{
		string returnType = metadata.FieldName;
		if(!metadata.IsCollection)
		{
			returnType = returnType + ".Entity";
		}

this.Write("get\r\n{\r\n\tif(this.");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.FieldName));

this.Write(" == null)\r\n\t{\r\n\t\tthis.");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.FieldName));

this.Write(" = new ");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.AssociationTypeName));

this.Write("(this, \"");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.PropertyName));

this.Write("\", this.Filter");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.PropertyName));

this.Write(");\r\n\t}\r\n\treturn this.");

this.Write(this.ToStringHelper.ToStringWithCulture(returnType));

this.Write(";\r\n}\r\n");


	}
	
	private void GenerateSingletonAssociationPropertySetter(AssociationMetadata metadata)
	{
		if (metadata.IsExternal && !metadata.AssociationAttribute.IsForeignKey)
        {
			return;
		}
		

this.Write("set\r\n");

  	this.GenerateOpeningBrace();

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.PropTypeName));

this.Write(" previous = this.");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.PropertyDescriptor.Name));

this.Write(";\r\nif (previous != value)\r\n");

  	this.GenerateOpeningBrace();

this.Write("this.ValidateProperty(\"");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.PropertyDescriptor.Name));

this.Write("\", value);\r\n");


		PropertyDescriptor reverseAssociationMember = GetReverseAssociation(metadata.PropertyDescriptor, metadata.AssociationAttribute);
		
		bool reverseIsSingleton = false;
		bool isBiDirectionalAssociation = (reverseAssociationMember != null) && this.CanGenerateProperty(reverseAssociationMember);	
		string revName = isBiDirectionalAssociation ? reverseAssociationMember.Name : string.Empty;
		if(isBiDirectionalAssociation && !metadata.IsExternal)
		{

this.Write("if(previous != null)\r\n");


			this.GenerateOpeningBrace();

this.Write("this.");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.FieldName));

this.Write(".Entity = null;\r\n");


			reverseIsSingleton = !EntityGenerator.IsCollectionType(reverseAssociationMember.PropertyType);
			if(!reverseIsSingleton)
			{

this.Write("previous.");

this.Write(this.ToStringHelper.ToStringWithCulture(revName));

this.Write(".Remove(this);\r\n");


			}
			else
			{

this.Write("previous.");

this.Write(this.ToStringHelper.ToStringWithCulture(revName));

this.Write(" = null;\r\n");


			}
				
			this.GenerateClosingBrace();
		}
		
		if(metadata.AssociationAttribute.IsForeignKey)
		{
			string[] thisKeyProps = metadata.AssociationAttribute.ThisKeyMembers.ToArray();
    	    string[] otherKeyProps = metadata.AssociationAttribute.OtherKeyMembers.ToArray();

this.Write("if(value != null)\r\n");


			this.GenerateOpeningBrace();
			for(int i = 0; i < thisKeyProps.Length; i++)
			{

this.Write("this.");

this.Write(this.ToStringHelper.ToStringWithCulture(thisKeyProps[i]));

this.Write(" = value.");

this.Write(this.ToStringHelper.ToStringWithCulture(otherKeyProps[i]));

this.Write(";\r\n");


			}
			this.GenerateClosingBrace();

this.Write("else\r\n");


			this.GenerateOpeningBrace();
			for(int i = 0; i < thisKeyProps.Length; i++)
			{
				Type foreignKeyType = TypeDescriptor.GetProperties(this.Type).Find(thisKeyProps[i], false).PropertyType;
				string foreignKeyTypeName = CodeGenUtilities.GetTypeName(foreignKeyType);

this.Write("this.");

this.Write(this.ToStringHelper.ToStringWithCulture(thisKeyProps[i]));

this.Write(" = default(");

this.Write(this.ToStringHelper.ToStringWithCulture(foreignKeyTypeName));

this.Write(");\r\n");


			}			
			this.GenerateClosingBrace();
			
			if(!metadata.IsExternal)
			{
				this.GenerateSingletonAssociationPropertySetterEntitySetStatement(metadata, isBiDirectionalAssociation, reverseIsSingleton, revName);
			}
		}
		else
		{
			this.GenerateSingletonAssociationPropertySetterEntitySetStatement(metadata, isBiDirectionalAssociation, reverseIsSingleton, revName);			
		}
		
		if(!metadata.IsExternal)
		{

this.Write("this.RaisePropertyChanged(\"");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.PropertyDescriptor.Name));

this.Write("\");\r\n");


		}
		this.GenerateClosingBrace();
		this.GenerateClosingBrace();
	}

	private void GenerateSingletonAssociationPropertySetterEntitySetStatement(AssociationMetadata metadata, bool isBiDirectionalAssociation, bool reverseIsSingleton, string revName)
	{

this.Write("this.");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.FieldName));

this.Write(".Entity = value;\r\n");


		if(isBiDirectionalAssociation)
		{

this.Write("if(value != null)\r\n");


			this.GenerateOpeningBrace();
			if(!reverseIsSingleton)
			{

this.Write("value.");

this.Write(this.ToStringHelper.ToStringWithCulture(revName));

this.Write(".Add(this);\r\n");


			}
			else
			{

this.Write("value.");

this.Write(this.ToStringHelper.ToStringWithCulture(revName));

this.Write(" = this;\r\n");


			}
			this.GenerateClosingBrace();
			
		}
	}
	
	private void GenerateAssociationFilterMethod(AssociationMetadata metadata)
	{		
		string[] thisKeyProps = metadata.AssociationAttribute.ThisKeyMembers.ToArray();
        string[] otherKeyProps = metadata.AssociationAttribute.OtherKeyMembers.ToArray();

this.Write("private bool Filter");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.PropertyName));

this.Write("(");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.PropTypeName));

this.Write(" entity)\r\n");

 	this.GenerateOpeningBrace(); 

this.Write("return (");

  
		for(int i = 0; i < thisKeyProps.Length; i++)
		{

this.Write("entity.");

this.Write(this.ToStringHelper.ToStringWithCulture(otherKeyProps[i]));

this.Write(" == this.");

this.Write(this.ToStringHelper.ToStringWithCulture(thisKeyProps[i]));


			if(i + 1 < thisKeyProps.Length)
			{
				
this.Write(" && ");


			}
		}

this.Write(");\r\n");


		this.GenerateClosingBrace();
	}
	
	private void GenerateCollectionSideAssociation(PropertyDescriptor pd)
	{
		AssociationMetadata metadata = new AssociationMetadata(pd);
		this.GenerateAssociationField(metadata);
		this.GenerateCollectionAssociationProperty(metadata);
		this.GenerateAssociationFilterMethod(metadata);		
	}
	
	private void GenerateCollectionAssociationProperty(AssociationMetadata metadata)
	{
		this.GenerateAssociationPropertyDeclaration(metadata);
		this.GenerateOpeningBrace();
		bool isBiDirectionalCollection = this.GenerateCollectionAssociationPropertyGetter(metadata);
		this.GenerateClosingBrace();
		
		if(isBiDirectionalCollection)
		{
			this.GenerateAttachMethod(metadata);
			this.GenerateDetachMethod(metadata);
		}
	}
	
	private bool GenerateCollectionAssociationPropertyGetter(AssociationMetadata metadata)
	{
		PropertyDescriptor reverseAssociationMember = GetReverseAssociation(metadata.PropertyDescriptor, metadata.AssociationAttribute);
		
		bool isBiDirectionalAssociation = (reverseAssociationMember != null) && this.CanGenerateProperty(reverseAssociationMember);
		string revName = isBiDirectionalAssociation ? reverseAssociationMember.Name : string.Empty;

		bool isBiDirectionalCollection = isBiDirectionalAssociation && metadata.IsCollection;
		
		string detachMethodName = "Detach" + metadata.PropertyName;
		string attachMethodName = "Attach" + metadata.PropertyName;

this.Write("get\r\n");


		this.GenerateOpeningBrace();	

this.Write("if(this.");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.FieldName));

this.Write(" == null)\r\n");


		this.GenerateOpeningBrace();	

this.Write("this.");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.FieldName));

this.Write(" = new ");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.AssociationTypeName));

this.Write("(this, \"");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.PropertyName));

this.Write("\", this.Filter");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.PropertyName));


		if(isBiDirectionalCollection)
		{

this.Write(", this.");

this.Write(this.ToStringHelper.ToStringWithCulture(attachMethodName));

this.Write(", this.");

this.Write(this.ToStringHelper.ToStringWithCulture(detachMethodName));

			
		}

this.Write(");\r\n");


		this.GenerateClosingBrace();

this.Write("return this.");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.FieldName));

this.Write(";\r\n");


		this.GenerateClosingBrace();
		
		return isBiDirectionalCollection;
	}
	
	private void GenerateAttachMethod(AssociationMetadata metadata)
	{
		PropertyDescriptor reverseAssociationMember = GetReverseAssociation(metadata.PropertyDescriptor, metadata.AssociationAttribute);		
		string revName = reverseAssociationMember.Name;
		string attachMethodName = "Attach" + metadata.PropertyName;

this.Write("private void ");

this.Write(this.ToStringHelper.ToStringWithCulture(attachMethodName));

this.Write("(");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.PropTypeName));

this.Write(" entity)\r\n");


		this.GenerateOpeningBrace();		
		if(!metadata.IsCollection)
		{

this.Write("entity.");

this.Write(this.ToStringHelper.ToStringWithCulture(revName));

this.Write(".Add(this);\r\n");


		}
		else
		{

this.Write("entity.");

this.Write(this.ToStringHelper.ToStringWithCulture(revName));

this.Write(" = this;\r\n");


		}		
		this.GenerateClosingBrace();
	}
	
	private void GenerateDetachMethod(AssociationMetadata metadata)
	{
		PropertyDescriptor reverseAssociationMember = GetReverseAssociation(metadata.PropertyDescriptor, metadata.AssociationAttribute);		
		string revName = reverseAssociationMember.Name;
		string detachMethodName = "Detach" + metadata.PropertyName;

this.Write("private void ");

this.Write(this.ToStringHelper.ToStringWithCulture(detachMethodName));

this.Write("(");

this.Write(this.ToStringHelper.ToStringWithCulture(metadata.PropTypeName));

this.Write(" entity)\r\n");


		this.GenerateOpeningBrace();		
		if(!metadata.IsCollection)
		{

this.Write("entity.");

this.Write(this.ToStringHelper.ToStringWithCulture(revName));

this.Write(".Remove(this);\r\n");


		}
		else
		{

this.Write("entity.");

this.Write(this.ToStringHelper.ToStringWithCulture(revName));

this.Write(" = null;\r\n");


		}		
		this.GenerateClosingBrace();	
	}

    }
}
