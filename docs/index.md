---
title: Open Ria Services
TOCTitle: Open Ria Services
ms:assetid: 3e32ac52-9d4f-4a5e-9a98-05cc0348bf17
ms:mtpsurl: https://msdn.microsoft.com/en-us/library/Ee707344(v=VS.91)
ms:contentKeyID: 27195652
ms.date: 08/19/2013
mtps_version: v=VS.91
---

# Open Ria Services

\[ **This document was written for WCF Services Version 1 Service Pack 2 and might not be up to date** <br />
Please see [Release Notes](https://github.com/OpenRIAServices/OpenRiaServices/releases) or [Changelog](https://github.com/OpenRIAServices/OpenRiaServices/blob/main/Changelog.md) for a list of changes since WCF RIA Services \]

Open Ria Services simplifies the development of n-tier solutions for Rich Internet Applications (RIA), such as Silverlight applications. A common problem when developing an n-tier RIA solution is coordinating application logic between the middle tier and the presentation tier. To create the best user experience, you want your Open Ria Services client to be aware of the application logic that resides on the server, but you do not want to develop and maintain the application logic on both the presentation tier and the middle tier. Open Ria Services solves this problem by providing framework components, tools, and services that make the application logic on the server available to the Open Ria Services client without requiring you to manually duplicate that programming logic. You can create a Open Ria Services client that is aware of business rules and know that the client is automatically updated with latest middle tier logic every time that the solution is re-compiled.

The following illustration shows a simplified version of an n-tier application. Open Ria Services focuses on the box between the presentation tier and the data access layer (DAL) to facilitate n-tier development with a Open Ria Services client.

![Open Ria Services n-tier application](./images\Ee707344.RIA_Overview.png "Open Ria Services n-tier application")

Open Ria Services adds tools to Visual Studio that enable linking client and server projects in a single solution and generating code for the client project from the middle-tier code. The framework components support prescriptive patterns for writing application logic so that it can be reused on the presentation tier. Services for common scenarios, such as authentication and user settings management, are provided to reduce development time.

## WCF Integration

In Open Ria Services, you expose data from the server project to client project by adding domain services. The Open Ria Services framework implements each domain service as a Windows Communication Foundation (WCF) service. Therefore, you can apply the concepts you know from WCF services to domain services when customizing the configuration. For more information, see [Domain Services](./ee707373).

## Securing a Open Ria Services Solution

To ensure that your application addresses the security concerns associated with exposing a domain service, you must carefully consider how you implement the domain service. For more information, see [Building Secure Applications with Open Ria Services](./ff626373).

## Tools and Documentation

The Open Ria Services documentation require several prerequisite programs, such as Visual Studio and the Silverlight Developer Runtime and SDK, be installed and configured properly, in addition to Open Ria Services and the Open Ria Services Toolkit to work thorough the walkthroughs and how-to topics. They also require installing and configuring SQL Server 2008 R2 Express with Advanced Services and installing the AdventureWorks OLTP and LT database.

Detailed instructions for the satisfaction of each of these prerequisites are provided by the topics within the [Prerequisites for Open Ria Services](./gg512106) node. Follow the instructions provided there before proceeding with this walkthrough to ensure that you encounter as few problems as possible when working through this Open Ria Services walkthroughs.

## Topics

[Prerequisites for Open Ria Services](./gg512106)

  - [Walkthrough: Installing and Configuring SQL Server 2008 R2 Express with Advanced Services](./gg512108)

  - [Walkthrough: Installing the AdventureWorks OLTP and LT sample databases](./gg512107)

[Creating Open Ria Services Solutions](./ee707336)

  - [Walkthrough: Taking a Tour of Open Ria Services](./ff713719)

  - [Walkthrough: Creating a Open Ria Services Solution](./ee707376)

  - [Walkthrough: Creating a Open Ria Service with the Code First Approach](./hh556025)

  - [Walkthrough: Using the Silverlight Business Application Template](./ee707360)

  - [Walkthrough: Creating a Open Ria Services Class Library](./ee707351)

  - [Walkthrough: Localizing a Business Application](./ff679940)

  - [How to: Create a Domain Service that uses POCO-defined Entities](./gg602754)

  - [How to: Add or Remove a Open Ria Services Link](./ee707372)

  - [Using the Domain Service Wizard](./gg153664)

[Building Secure Applications with Open Ria Services](./ff626373)

[Deploying and Localizing a Open Ria Services Solutions](./ff679939)

  - [Troubleshooting the Deployment of a Open Ria Services Solution](./ff426913)

  - [Troubleshooting the Deployment of a Open Ria Services Solution](./ff426913)

  - [Walkthrough: Localizing a Business Application](./ff679940)

[Middle Tier](./ee707348)

  - [Domain Services](./ee707373)
    
      - [Walkthrough: Adding Query Methods](./ee707362)
    
      - [How to: Add Business Logic to the Domain Service](./ee796240)
    
      - [How to: Create a Domain Service that uses POCO-defined Entities](./gg602754)
    
      - [How to: Use HTTPS with a Domain Service](./ee707342)

  - [Data](./ee707356)
    
      - [Compositional Hierarchies](./ee707346)
    
      - [Presentation Models](./ee707347)
    
      - [Inheritance in Data Models](./ee707366)
    
      - [Complex Types](./gg602753)
    
      - [Shared Entities](./gg602750)
    
      - [Walkthrough: Sharing Entities between Multiple Domain Services](./ff422034)
    
      - [How to: Add Metadata Classes](./ee707339)
    
      - [How to: Validate Data](./ee707335)
    
      - [Managing Data Concurrency](./gg602751)
    
      - 1.  [How to: Enable Optimistic Concurrency Checks](./gg602748)
        
        2.  [How to: Add Explicit Transactions to a Domain Service](./ee707364)

  - [Shared Code](./ee707371)
    
      - [How to: Share Code through Source Files](./ee707369)
    
      - [Walkthrough: Creating a Open Ria Services Class Library](./ee707351)

[Silverlight Clients](./ee707349)

  - [Client Code Generation](./ee707359)

  - [DomainContext and Operations](./ee707370)

  - [DomainDataSource](./ee707363)

  - [Error Handling on the Client](./ee807307)

  - [Customizing Generated Code](./ee707345)
    
      - [How to: Add Computed Properties on the Client](./ee707331)

[Accessing non-Silverlight Clients](./gg602749)

  - [ASP.NET Clients](./ee707352)

  - [Walkthrough: Using the Domain Service in ASP.NET Applications](./ee807305)

[Authentication, Roles, and Profiles](./ee707361)

  - [How to: Enable Authentication in Open Ria Services](./ee707353)

  - [How to: Enable Roles in Open Ria Services](./ee707375)

  - [How to: Enable Profiles in Open Ria Services](./ee707350)

  - [How to: Create a Custom Authorization Attribute](./ee707357)

  - [Walkthrough: Using Authentication Service with Silverlight Business Application](./ee942449)

  - [Walkthrough: Using Authentication Service with Silverlight Navigation Application](./ee942451)

[End-to-EndScenarios](./gg602747)

  - [Walkthrough: Retrieving and Displaying Data From a Domain Service](./ee707367)

  - [Walkthrough: Editing Data From a Domain Service](./ee707338)

  - [Walkthrough: Displaying Data in a Silverlight Business Application](./ee796239)

  - [Walkthrough: Displaying Related Data in a Silverlight Business Application](./ee796241)

[Reference](./ee707374)

## See Also

#### Other Resources

[Offline Open Ria Services documentation](http://go.microsoft.com/fwlink/?linkid=185200)

