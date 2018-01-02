**This can be seen as a draft and should probably be improved more in the future**


## General feedback or discussions

Use the the open ria services forum at  openriaservices.net [http://openriaservices.net/MonoX/Pages/SocialNetworking/Discussion.aspx](http://openriaservices.net/MonoX/Pages/SocialNetworking/Discussion.aspx)

## I've found a bug or have a feature request

Feel free to post an issue here on codeplex. 
If you are uncertain if something really is a bug or not then you can try to ask at the open ria services forum at  openriaservices.net [http://openriaservices.net/MonoX/Pages/SocialNetworking/Discussion.aspx](http://openriaservices.net/MonoX/Pages/SocialNetworking/Discussion.aspx)

## Reporting an issue.

Please have a look at the good bug filing template for asp.net  found at [https://github.com/aspnet/Home/wiki/Functional-bug-template](https://github.com/aspnet/Home/wiki/Functional-bug-template)


## First steps in contributing code or other content

If you are interested in contributed to the project you will most likely need to sign a CLA  (Contrbutor License Agreement). In order to to so you contact contributions@outercurve,org and let them know you want to sign the CLA agreement for Open RIA Services. They will send you details and ask which version of the CLA you need to sign (if you will do the contribution at work or indiviually).

Download the code, compile and have a look around.

What to contribute with:

* If you are starting you can look for issues marked as Up For Grabs or which looks simple.
* If you find an issue which you want to provide a fix for then please do add a comment that you want to or will investigate it. 
* The documentation is currently very sparse and lacking, please feel free to contribute.  You can always post issues with proposed documentation.
* Not all tests currently run and some are non-trivial to get going, so any help in getting more tests passing or improving the getting started experience are welcome.

### Code style

Try to follow the existing coding style.
Make sure that you can compile the project with your changes using release build without any new compilation warning.

### Comments

All new public, protected and internal methods should be documented, using standard "{{///}}" comments.
Make sure that you can compile the project with your changes using release build without any new compilation warning.

### Commit messages

* Make sure that the first line always contains a short **descriptive** summary of the changes.
* The remainting rows can be used to provide more details about the changes.
* You can use "Fix # BUG TITLE" if bug title is describing and the contribution is a "small" one-commit pull request
* If the commits 


Example of a commit message for a fix which only containst a single commit:
{{
Fix #X THE BUG DESCRIPTION /summary of changes
 Commit detail 1
 Commit detail 2
 Commit detail 3
}}

For multi part commits, it is a good idea to embed the issue number in the commit message, but it is not mandatory at the moment.
{{
[Issue #232](Issue-#232) Short summary of changes
 Commit detail 1
 Commit detail 2
 Commit detail 3
}}

### Tests

Please take your time and add test for any new functionality added.
The current test.
