# PuppeteerSharp�D����DLL??���`���

## ??�y�z
��?�b?��]�tPuppeteerSharp��?��?�J��??�G
```
? ?��??: error CS0009: ?�k��?��?�u���"C:\Users\26441\.nuget\packages\puppeteersharp\20.2.0\lib\net8.0\ChromeHeadlessShell\Win64-138.0.7204.92\chrome-headless-shell-win64\vulkan-1.dll"-- PE �M�����]�t���󦫺ޤ�?�u�C
```

## �ڥ���]
PuppeteerSharp��?�NNuGet�]�]�t�j�q�D���ު����DLL���]�pvulkan-1.dll�Bchrome-headless-shell���^�A��TaskAssistant���{�Ƕ��[??????�Ҧ�.dll��󳣧@?.NET���޵{�Ƕ��[?�A?�P"PE�M�����]�t���ޤ�?�u"??�C

## ��?���

### 1. ����D����DLL??
�b`ScriptExecutionService.cs`���K�[�F`IsUnmanagedDll()`��k�A����H�U??���G

#### ���W�Ҧ�??
- Chrome/Chromium��?�Gchrome-headless-shell, chromium, chrome_, libcef, cef_
- ?�δ�V��?�Gvulkan, d3d, opengl, gpu, swiftshader, angle
- ��?/??��?�Gffmpeg, avcodec, avformat, avutil, swscale
- �I?��?�Gcurl, ssl, crypto, zlib
- Windows?��?�Gapi-ms-, ucrtbase, vcruntime, msvcp, msvcr
- �q�ΫD���޼Ҧ��G_native, .native, native_, win32, x64, x86

#### ��?��???
?�d���O�_��_�i��]�t�D����DLL����?�G
- native, unmanaged, win-x64, win-x86, runtimes, chromium, chrome, browser

#### PE?�ֳt?�d
?�i�ä��?��?�q?PE??�d�A�קK����[?�C

### 2. �W?���{�Ƕ��[???
?�b�{�Ƕ��[???��????�G
```
?? �{�Ƕ��[???:
   ? ���\: 15 ?
   ?? ��?: 127 ? (�D����DLL)
   ? ��?: 0 ?
   ?? ���\�v: 100.0%
```

### 3. ɬ�ƪ�???�z
- ��???�}��?�D����DLL
- �S��?�z"PE�M�����]�t���ޤ�?�u"??
- ����??����?��]?��

### 4. ��s�q?�ư��Ҧ�
�b`AppSettingsService.cs`���W?�F�q?�ư��Ҧ��A�]�t�G
- Chrome/Chromium��?�Ҧ�
- ?�δ�V?�Ҧ�
- �h�C�^?�z?�Ҧ�
- �t??��??�Ҧ�

## ��???�S?

### �ʯ�ɬ��
- �ϥΧֳt���W�ǰt�A�קK�����n�����?��
- ?�q?PE??�d�A������[?���
- ����?�s???�G

### ??���`
- �h???���G���W �� ��?��? �� PE? �� �ݱ`��?
- �Y��??��?�A�]��q?�ݱ`?�z���̸�?
- ���v?���`���޵{�Ƕ����[?

### ��?�^?
- ??��?��?�X�A��?�F��?�z?�{
- �M����??�H���A?�ܦ��\�v
- �ͦn��??���ܩM��??��

## ????

?�ؤF`PuppeteerSharpTest.csx`?���Τ_??���`�ĪG�G
- ??PuppeteerSharp�]���U?�M�[?
- ??�D����DLL??���
- ��?�\�ॿ�`?��

## ?���ĪG

���`�Z��???��ݨ�?���H�U?�X�G
```
?? ?�l?�z 1 ? NuGet �]�ޥ�...
?? [1/1] ���b?�z�]: PuppeteerSharp
?? ?�l�U?�]: PuppeteerSharp v20.2.0
? �U?����: PuppeteerSharp v20.2.0
?? ���b?�]���[?�{�Ƕ�...
?? ���b�[?: PuppeteerSharp.dll... ? (2.1 MB)
?? ��?�D����DLL: vulkan-1.dll
?? ��?�D����DLL: chrome-headless-shell.exe
?? �{�Ƕ��[???:
   ? ���\: 1 ?
   ?? ��?: 127 ? (�D����DLL)
   ? ��?: 0 ?
? �]?�z���\: PuppeteerSharp
```

?�bTaskAssistant��?����?�z�]�t�D����DLL���`?NuGet�]�A?��?���ѧ�n��?��?���^?�C