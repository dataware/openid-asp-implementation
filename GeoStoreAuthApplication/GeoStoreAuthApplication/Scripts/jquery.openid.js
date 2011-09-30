//jQuery OpenID Plugin 1.1 Copyright 2009 Jarrett Vance http://jvance.com/pages/jQueryOpenIdPlugin.xhtml
$.fn.openid = function () {
    var $this = $(this);
    var $opendiv = $this.find('div.openid');
    var $usr = $opendiv.find('input[name=openid_username]');
    var $id = $opendiv.find('input[name=openid_identifier]');
    var $front = $opendiv.find('div:has(input[name=openid_username])>span:eq(0)');
    var $end = $opendiv.find('div:has(input[name=openid_username])>span:eq(1)');
    var $usrfs = $opendiv.find('div:has(input[name=openid_username])');
    var $idfs = $opendiv.find('div:has(input[name=openid_identifier])');

    //open id methods
    var validateusr = function () {
        if ($usr.val().length < 1) {
            $usr.focus();
            return false;
        }
        $id.val($front.text() + $usr.val() + $end.text());
        return true;
    };

    var validateid = function () {
        if ($id.val().length < 1) {
            $id.focus();
            return false;
        }
        return true;
    };

    var direct = function () {
        var $li = $(this);
        $li.parent().find('li').removeClass('highlight');
        $li.addClass('highlight');
        $usrfs.fadeOut();
        $idfs.fadeOut();
        updateRedirectionSpan($li);
        return false;
    };

    var openid = function () {
        var $li = $(this);
        $li.parent().find('li').removeClass('highlight');
        $li.addClass('highlight');
        $usrfs.hide();
        $idfs.show();
        $id.focus();
        updateRedirectionSpan($li);
        return false;
    };

    var username = function () {
        var $li = $(this);
        $li.parent().find('li').removeClass('highlight');
        $li.addClass('highlight');
        $idfs.hide();
        $usrfs.show();
        $this.find('label[for=openid_username] span').text($li.attr("title"));
        $front.text($li.find("span").text().split("username")[0]);
        $end.text("").text($li.find("span").text().split("username")[1]);
        $id.focus();
        updateRedirectionSpan($li);
        return false;
    };

    function updateRedirectionSpan(item) {
        $('div.redirection_caption > span').text(item.attr("data-custom-title"));
    }

    $id.keypress(function (e) {
        if ((e.which && e.which == 13) || (e.keyCode && e.keyCode == 13)) {
            return submitid();
        }
    });
    $usr.keypress(function (e) {
        if ((e.which && e.which == 13) || (e.keyCode && e.keyCode == 13)) {
            return submitusr();
        }
    });

    $opendiv.find('li.direct').click(direct);
    $opendiv.find('li.openid').click(openid);
    $opendiv.find('li.username').click(username);
    $opendiv.find('li span').hide();
    $opendiv.find('li').css('line-height', 0).css('cursor', 'pointer');
    $opendiv.find('li:eq(0)').click();


    //Validation Methods
    var updateUserNameValidationMessage = function (event) {
        var value = $.trim(event.target.value)
        if (value.length > 0) {
            $.getJSON('/Account/VerifyUser', $.param({ 'userName': event.target.value }),
        function (response) {
            $('#UserNameValidation').data('status', response.status);
            $('#UserNameValidation').html(response.message);
            if ($('#UserNameValidation').is(':hidden'))
                $('#UserNameValidation').css("display", 'inline');
        });
        }
        else {
            $('#UserNameValidation').hide();
        }
    };

    var updateEmailValidationMessage = function (event) {
        var value = $.trim(event.target.value)
        if (value.length > 0) {
            $.getJSON('/Account/VerifyEmailAddress', $.param({ 'email': event.target.value }),
       function (response) {
           $('#EmailAddressValidation').data('status', response.status);
           $('#EmailAddressValidation').html(response.message);
           if ($('#EmailAddressValidation').is(':hidden'))
               $('#EmailAddressValidation').css("display", 'inline');
       });
        }
        else {
            $('#EmailAddressValidation').hide();
        }
    };

    function validateInput() {
        var validate = true;
        $userNameStatus = $('#UserNameValidation').data('status');
        if ($userNameStatus == 0) {
            $('input#UserName').focus();
            validate = false;
        }
        else if ($('#EmailAddressValidation').data('status') == 0) {
            $('input#Email').focus();
            validate = false;
        } else {
            var $selectedli = $opendiv.find('li.direct,li.username,li.openid').filter('.highlight');
            if ($selectedli.hasClass("direct")) {
                $id.val($selectedli.find("span").text());
            } else if ($selectedli.hasClass("openid")) {
                validate = validateid();
            } else if ($selectedli.hasClass("username")) {
                validate = validateusr();
            }
        }
        return validate;
    };

    $('#UserName').keyup(updateUserNameValidationMessage);
    $('#UserName').change(updateUserNameValidationMessage);

    $('#Email').keyup(updateEmailValidationMessage);
    $('#Email').change(updateEmailValidationMessage);

    $("input[type='submit']").click(validateInput);

    return this;
};