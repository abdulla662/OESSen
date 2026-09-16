window.downloadPdfFile = (fileName, pdfBytes) => {
    const blob = new Blob([new Uint8Array(pdfBytes)], {
        type: "application/pdf"
    });

    const url = window.URL.createObjectURL(blob);

    const link = document.createElement("a");
    link.href = url;
    link.download = `${fileName}.pdf`;

    document.body.appendChild(link);
    link.click();

    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
};
