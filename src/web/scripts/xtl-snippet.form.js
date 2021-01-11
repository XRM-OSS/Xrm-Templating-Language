(function(XtlSnippet, undefined) {
    const setXtlExpressionVisibility = (executionContext) => {
        const context = executionContext.getFormContext();

        const isPlainText = context.getAttribute("oss_containsplaintext").getValue();
        const isHtml = context.getAttribute("oss_ishtml").getValue();

        const showRichTextControl = isPlainText && isHtml;

        context.getControl("oss_xtlexpression").setVisible(!showRichTextControl);
        context.getControl("oss_xtlexpression1").setVisible(showRichTextControl);
    };

    const setIsHtmlVisibility = (executionContext) => {
        const context = executionContext.getFormContext();

        const isPlainText = context.getAttribute("oss_containsplaintext").getValue();

        context.getControl("oss_ishtml").setVisible(isPlainText);
    };


    XtlSnippet.onLoad = (executionContext) => {
        const context = executionContext.getFormContext();

        context.getAttribute("oss_containsplaintext").addOnChange(setXtlExpressionVisibility);
        context.getAttribute("oss_containsplaintext").addOnChange(setIsHtmlVisibility);
        context.getAttribute("oss_ishtml").addOnChange(setXtlExpressionVisibility);

        setXtlExpressionVisibility(executionContext);
        setIsHtmlVisibility(executionContext);
    };

})(window.XtlSnippet = window.XtlSnippet || {});