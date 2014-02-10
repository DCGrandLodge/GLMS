(function ($) {
    if (!Array.prototype.filter) {
        Array.prototype.filter = function (fun /*, thisp*/) {
            var len = this.length;
            if (typeof fun != "function")
                throw new TypeError();

            var res = new Array();
            var thisp = arguments[1];
            for (var i = 0; i < len; i++) {
                if (i in this) {
                    var val = this[i]; // in case fun mutates this
                    if (fun.call(thisp, val, i, this))
                        res.push(val);
                }
            }

            return res;
        };
    }

    $.fn.scrollTo = function () {
        return this.each(function () {
            var body = (window.opera) ? (document.compatMode == "CSS1Compat" ? $('html') : $('body')) : $('html,body');
            body.animate({ scrollTop: $(this).offset().top }, 0);
        });
    }

    $.fn.center = function (options) {
        var settings = $.extend({ position: 'absolute' }, options);
        this.css({ top: '50%', left: '50%', margin: '-' + (this.height() / 2) + 'px 0 0 -' + (this.width() / 2) + 'px' });
        this.css("position", settings.position);
        return this;
    }

    $.fn.applyStyles = function () {
        $('.button,input[type="submit"],input[type="button"],input[type="reset"],button,#commands a,.footer-tools a,.header-tools a', this).button();
    }

    $.GLMS = $.GLMS ||
    {
        loading: function (caption) {
            var zmax = 0;
            $('#ajax-loading').siblings().each(function () {
                var cur = this.style.zIndex;
                zmax = cur > zmax ? cur : zmax;
            });
            $('#ajax-loading p').text((caption ? caption : 'Loading') + ', please wait...');
            $('#ajax-loading').css('zIndex', zmax + 1).show();
        },
        loaded: function () { $('#ajax-loading').hide().css('zIndex', 0); },

        saveForm: function (e) {
            e.preventDefault();
            if ($('form').first().validate().form()) {
                $('form').first().submit();
            }
        },

        setupMultiselect: function (options) {
            options = $.extend({
                loading: false,
                noneSelectedText: 'Ignore',
                showNoneSelected: true,
                width: 225
            }, options);
            options.name = options.name ? options.name : options.id.toLowerCase();
            var memo = $("ul#" + options.name + "-list");
            var list = $('#' + options.id);
            var select = $("select#" + options.id).multiselect({
                minWidth: options.width,
                selectedList: 0,
                noneSelectedText: options.noneSelectedText,
                click: function (event, ui) {
                    if (ui.checked || options.loading) {
                        $("li.list-none", memo).remove();
                        var li = $('<li class="memo-label"/>').attr('id', ui.value).text(ui.text);
                        li.appendTo(memo);
                    }
                    else {
                        $("li#" + ui.value, memo).remove();
                        if (options.showNoneSelected && memo.children().length <= 0) {
                            $('<li class="list-none">' + options.noneSelectedText + '</li>').appendTo(memo);
                        };
                    }
                },
                checkAll: function () {
                    memo.children().remove();
                    $.each($('option', list), function () {
                        var li = $('<li class="memo-label"/>').attr('id', this.value).text(this.text);
                        li.appendTo(memo);
                    });
                },
                uncheckAll: function () {
                    memo.children().remove();
                    if (options.showNoneSelected) {
                        $('<li class="list-none">' + options.noneSelectedText + '</li>').appendTo(memo);
                    }
                }
            }).multiselectfilter();
            return select;
        },

        checkAjaxRedirect: function (data) {
            if (data.redirectUrl) {
                switch (data.reason) {
                    case "logon":
                        // TODO - Ajax-y logon
                        break;
                    case "nopermission":
                        // TODO - Ajax-y no permission
                        break;
                    default:
                        window.location = data.redirectUrl;
                }
            }
        },

        getMVCIdFromName: function (controlName) { return controlName.replace(/\./g, '_'); },

        getMVCNameFromId: function (controlId) { return controlId.replace(/_/g, '.'); },

        initialize: function () {
            $(document).ajaxSuccess(function (event, xhr, ajaxOptions) {
                if (xhr.responseText.match(/^{"redirectUrl":/)) {
                    $.GLMS.checkAjaxRedirect($.parseJSON(xhr.responseText));
                }
            });
            return this;
        }
    }.initialize();


    var GLMSMethods = {
        register: function (method, handler) {
            GLMSMethods[method] = handler;
        },
        init: function (arguments) { }
    };
    $.fn.GLMS = function (method) {
        // Method calling logic
        if (GLMSMethods[method]) {
            return GLMSMethods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return GLMSMethods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on jQuery.GLMS');
        }
    };
})(jQuery);


$.ajaxSetup({
    cache: false
});

if (typeof String.prototype.startsWith != 'function') {
    String.prototype.startsWith = function (str) {
        return this.slice(0, str.length) == str;
    };
}

$(function () {
    if ($.validator) {
        $.validator.setDefaults({
            focusCleanup: true,
            onkeyup: null,
            ignore: ":hidden:not('.validate-hidden')",  // Skip validation on visually hidden fields
            onfocusout: function (element, event) {
                if (!this.checkable(element) && (element.name in this.submitted || !this.optional(element))) {
                    this.element(element);
                }
            },
        });
    }
    if ($.jgrid) {
        $.extend($.jgrid.defaults, {
            cmTemplate: { title: false },
            altclass: 'altrow',
            altRows: true,
            mtype: 'GET',
            datatype: 'json',
            viewrecords: true,
            scrollrows: false,
            rowNum: -1,
            hoverrows: false,
            height: '100%', 
            viewsortcols: [false, 'vertical', true],
            loadError: function(xht, st, handler) { 
                $(document.body).css('font-size', '100%'); 
                $(document.body).html(xht.responseText); 
            } 
        });
        $.extend($.jgrid.nav, {
            edit: false,
            add: false,
            del: false,
            search: false,
            refresh: true,
            view: false,
            position: "left",
            cloneToTop: true
        });
    }
    if ($.leaveNotice) {
        $.leaveNotice.setDefaults({
            siteName: "GLMS",
            exitMessage: "<p><strong>You have selected a link to an external site.</strong><br/>  You will automatically be redirected in 5 seconds.</p>",
            timeOut: 5000
        });
    }
    if ($.datepicker) {
        $.datepicker.setDefaults({
            altformat: 'mm/dd/yyyy',
            dateformat: 'mm/dd/yyyy',
            appendText: ' (mm/dd/yyyy)',
            changeMonth: true,
            changeYear: true,
            autoSize: true,
            constrainInput: true,
            shortYearCutoff: '+5',
            yearRange: 'c-10:c+10',
            showOn: "both",
            buttonImage: rootPath ? (rootPath.toString() + 'Content/calendar.gif') : '/Content/calendar.gif',
            buttonImageOnly: true
        });
    }
});

(function ($) {
    function initialize(options) {
        var settings = {
            editClass: '.edit-popup',
            viewClass: '.view-popup',
            deleteClass: '.delete-popup',
            deleteConfirmationClass: '.delete-confirmation',
            rowId: 0,
            mapData: function (data) { },
            mapViewData: function (data) { },
            completed: function (data) {
                if (!data.result) {
                    alert(data.message);
                }
            }
        };
        if (options) { $.extend(settings, options); }
        var init = $(this);
        var dialogOptions = {
            modal: true,
            autoOpen: false,
            width: 'auto',
            height: 'auto',
            buttons: [{
                id: "button-save",
                text: "Save",
                click: function () {
                    $('#button-save').button('disable');
                    $('#button-cancel').button('disable');
                    $.GLMS.loading('Saving');
                    $('form', init).ajaxSubmit({
                        dataType: 'json',
                        success: function (data) {
                            if (data.result) {
                                $.GLMS.loaded();
                                init.dialog('close');
                                $(settings.gridID).trigger('reloadGrid');
                            } else {
                                //alert(data.message); // Already alerting in 'completed'
                            }
                            settings.completed(data);
                        },
                        error: function () {
                            alert('An error occured');
                        },
                        complete: function (data) {
                            $.GLMS.loaded();
                            $('#button-save').button('enable');
                            $('#button-cancel').button('enable');
                        }
                    });
                }
            },
            {
                id: "button-cancel",
                text: "Cancel",
                click: function () {
                    init.dialog('close');
                }
            }]
        };
        if (settings.dialogOptions) {
            $.extend(dialogOptions, settings.dialogOptions);
        }
        init.dialog(dialogOptions);

        if (settings.editClass) {
            $(settings.editClass).live('click', function (e) {
                e.preventDefault();
                var rowId = $(this).attr('data-rowid');
                if (settings.dataUrl) {
                    $.GLMS.loading();
                    $.ajax({
                        url: settings.dataUrl + '/' + rowId,
                        dataType: 'json',
                        type: 'get',
                        success: function (data) {
                            $('form', init).resetForm();
                            if (!data) {
                                data = {};
                            }
                            settings.mapData(data);
                            $.GLMS.loaded();
                            init.dialog('open');
                        },
                        complete: function () {
                            $.GLMS.loaded();
                        }
                    });
                } else {
                    $('form', init).resetForm();
                    init.dialog('open');
                    if (settings.mapData) {
                        settings.mapData()
                    }
                }
            });
        }
        if (settings.viewClass) {
            $(settings.viewClass).live('click', function (e) {
                e.preventDefault();
                var rowId = $(this).attr('data-rowid');
                $.GLMS.loading();
                $.ajax({
                    url: settings.dataUrl + '/' + rowId,
                    dataType: 'json',
                    type: 'get',
                    success: function (data) {
                        $('form', init).resetForm();
                        if (!data) {
                            data = {};
                        }
                        settings.mapViewData(data);
                        init.dialog('open');
                    },
                    complete: function () {
                        $.GLMS.loaded();
                    }
                });
            });
        }
        
        if (settings.deleteClass) {
            $(settings.deleteClass).live('click', function (e) {
                e.preventDefault();
                var rowId = $(this).attr('data-rowid');
                settings.rowId = rowId;
                $(settings.deleteConfirmationClass).data('rowId', rowId).dialog('open');
            });
            $(settings.deleteConfirmationClass).dialog({
                modal: true,
                autoOpen: false,
                buttons: {
                    "Delete": function () {
                        var rowId = $(this).data('rowId');
                        $(this).dialog('close');
                        $.GLMS.loading('Saving');
                        $.ajax({
                            url: settings.deleteUrl + '/' + rowId,
                            dataType: 'json',
                            success: function (data) {
                                settings.completed(data);
                            },
                            complete: function () {
                                $.GLMS.loaded();
                                $(settings.gridID).trigger('reloadGrid');
                            },
                            type: 'POST'
                        });
                    },
                    "Cancel": function () {
                        $(this).dialog('close');
                    }
                }
            });
        }
    }

    $.fn.popupEditor = function (arg) {
        initialize.apply(this, arguments);
    }
})(jQuery);
