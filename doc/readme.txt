========================================================================

                              T e n u t o                               
                                  -- RELAX NG validator for .NET --

========================================================================
                            Developed by RELAX NG project at SourceForge

Overview
--------

Tenuto is a RELAX NG validator for Common Language Runtime (CLR)
environment written in C#. Tenuto can be either used as a stand-alone
command line validation tool, or as a library to add RELAX NG validation
functionality into your application.



Usage
-----

"tenuto.exe" is a command-line version of the tool. You can run this tool
as:

> tenuto myschema.rng document1.xml document2.xml ...

Executing tenuto.exe without any parameter will show you the usage screen.


To use Tenuto from your application, please refer to
src/driver/src/Driver.cs file. Tenuto still leaves a lot to be desired with
regard to the usability from application, so any input is very welcome.



Status
------

Tenuto attempts to conform to the version 1.0 of RELAX NG spec. It passes
all of the James Clark test suite, so the support can be considered as
fairly complete. It doesn't support RELAX NG DTD compatibility spec at all.

This implementation also includes a casual implementation of W3C XML Schema
Part 2 as a datatype library. Refer to the source code for more details.
This implementation is really partial, and any help would be appreciated.

Although Tenuto uses the vendor-neutral datatype interface developed at
SourceForge RELAX NG project, it doesn't support "plug-n-play" mechanism
simply because we developers don't know how to implement it with CLR.
So right now, the only available datatype libraries are the built-in library
and W3C XML Schema Part 2. Any input on this issue is also very welcome.



Support
-------

Tenuto is maintained at RELAX NG Project at SourceForge
(http://relaxng.sourceforge.net/)

If you find any question, comment, bug, and/or suggestion, please post it
to XXXXX@relaxng.sourceforge.net. If yours is RELAX NG generic one, you
might want to use relax-ng-comment@lists.oasis-open.org (to subscribe to
this list, go to http://lists.oasis-open.org/ob/adm\.pl)



License
-------

Tenuto is distributed under the BSD license. See copying.txt for details.



$Version:$
