mergeInto(LibraryManager.library, 
{
 
  OpenFile: function ()
  {
    openFilePicker({
        fn: (e, files) => {
            return files[0];
        }
    })
  },
 
  OpenFolder: function () 
  {
    window.alert(Pointer_stringify(str));
  },
 
});

function openFilePicker({fn, accept, multiple} = {}) {
      const inpEle = document.createElement("input");
      inpEle.id = `__file_${Math.trunc(Math.random() * 100000)}`;
      inpEle.type = "file";
      inpEle.style.display = "none";
      // 文件类型限制
      accept && (inpEle.accept = accept);
      // 多选限制
      multiple && (inpEle.multiple = multiple);
      inpEle.addEventListener("change", event => fn.call(inpEle, event, inpEle.files), {once: true});
      inpEle.click();
    }