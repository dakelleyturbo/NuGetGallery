$(function () {
    'use strict';

    function configureCopyButton(id) {
        var copyButton = $('#' + id + '-button');
        copyButton.popover({ trigger: 'manual' });

        copyButton.click(function () {
            var text = $('#' + id + '-text').text().trim();
            window.nuget.copyTextToClipboard(text);
            copyButton.popover('show');
            setTimeout(function () {
                copyButton.popover('destroy');
            }, 1000);
        });
    }

    window.nuget.configureExpanderHeading("dependency-groups");
    window.nuget.configureExpander("version-history");
    window.nuget.configureExpander(
        "hidden-versions",
        "CalculatorAddition",
        "Show less",
        "CalculatorSubtract",
        "Show more");    

    configureCopyButton('package-manager');
    configureCopyButton('dotnet-cli');
});