(function () {
    function getCanvasImage(canvas) {
        if (!canvas || canvas.width === 0 || canvas.height === 0) {
            return null;
        }

        try {
            return canvas.toDataURL('image/png');
        } catch (error) {
            console.warn('canvas image export failed', error);
            return null;
        }
    }

    function cloneSectionForPrint(section) {
        const clone = section.cloneNode(true);
        const sourceCanvases = section.querySelectorAll('canvas');
        const clonedCanvases = clone.querySelectorAll('canvas');

        clonedCanvases.forEach(function (canvas, index) {
            const imageSource = getCanvasImage(sourceCanvases[index]);
            if (!imageSource) {
                const fallback = document.createElement('div');
                fallback.className = 'text-muted small';
                fallback.textContent = 'グラフ画像を出力できませんでした。';
                canvas.replaceWith(fallback);
                return;
            }

            const image = document.createElement('img');
            image.src = imageSource;
            image.alt = 'グラフ';
            image.className = 'print-chart-image';
            canvas.replaceWith(image);
        });

        return clone;
    }

    function buildPrintCss() {
        return [
            '@page { size: A4 landscape; margin: 8mm; }',
            'body { padding: 8px; background: #fff; color: #212529; font-size: 11px; }',
            '.print-title { font-size: 15px; margin: 0 0 4px 0; }',
            '.print-meta { color: #6c757d; font-size: 9px; margin: 0 0 8px 0; }',
            '.weekly-report-sample, .weekly-timeline-sample { padding: 8px; border: 1px solid #dee2e6; margin-bottom: 6px; border-radius: 0; }',
            '.weekly-report-sample h2, .weekly-timeline-sample h2 { font-size: 12px; margin-bottom: 2px; }',
            '.weekly-report-sample p, .weekly-timeline-sample p { font-size: 8.5px; margin-bottom: 0; }',
            '.weekly-report-chart, .weekly-timeline-chart { min-height: 0 !important; }',
            // max-width + height: auto で縦横比を維持（width: 100% だとmax-heightと競合して縦つぶれが起きる）
            '.print-chart-image { max-width: 100%; height: auto; display: block; margin: 0 auto; }',
            '.weekly-report-sample .print-chart-image { max-height: 90mm; }',
            '.weekly-timeline-sample .print-chart-image { max-height: 140mm; }',
            // 通所スケジュールのベーススタイル（Razorインラインstyleがprint windowに読み込まれないため必須）
            '.weekly-schedule-strip { display: grid; grid-template-columns: repeat(7, minmax(56px, 1fr)); gap: 0.2rem; overflow: visible; font-size: 8.5px; margin-top: 6px; padding-bottom: 0; }',
            '.schedule-day { border: 1px solid #dee2e6; border-radius: 6px; padding: 0.2rem; display: flex; flex-direction: column; gap: 0.2rem; min-height: 46px; white-space: normal; }',
            '.schedule-day.planned { background: #E7F1FF; border-color: #9EC5FE; }',
            '.schedule-day.remote { background: #E8F6EF; border-color: #A3CFBB; }',
            '.schedule-day.rest { background: #F8F9FA; border-color: #CED4DA; }',
            '.schedule-date { font-size: 7.5px; color: #6c757d; }',
            '.schedule-entry { border-radius: 5px; padding: 0.1rem 0.2rem; display: flex; flex-direction: column; gap: 0.05rem; background: rgba(255,255,255,0.75); border-left: 4px solid #adb5bd; }',
            '.schedule-entry strong { font-size: 8.5px; line-height: 1.2; display: block; }',
            '.schedule-entry small { font-size: 7.5px; color: #495057; display: block; }',
            '.schedule-entry.status-attended { border-left-color: #198754; }',
            '.schedule-entry.status-planned { border-left-color: #0d6efd; }',
            '.schedule-entry.status-late { border-left-color: #fd7e14; }',
            '.schedule-entry.status-early-leave { border-left-color: #6f42c1; }',
            '.schedule-entry.status-absent { border-left-color: #dc3545; }',
            '.schedule-entry.status-none { border-left-color: #adb5bd; }',
            // record-noteのベーススタイル
            '.record-note { display: grid; grid-template-columns: 3.5rem 1fr; gap: 0.2rem; font-size: 8px; padding: 1px 0; border-bottom: 1px solid #edf0f2; }',
            '.record-note:last-child { border-bottom: 0; }',
            '.record-note strong { color: #495057; font-size: 8px; }',
            '.record-note span { font-size: 8px; min-width: 0; }',
            '.record-memo { display: block; color: #6c757d; margin-top: 1px; font-size: 7.5px; }',
            '.weekly-report-table { font-size: 8.5px !important; width: 100%; table-layout: fixed; }',
            '.weekly-report-table th, .weekly-report-table td { min-width: 0 !important; padding: 2px 4px !important; white-space: normal !important; word-break: break-word; vertical-align: top; overflow: hidden; }',
            '.weekly-report-table th:nth-child(1), .weekly-report-table td:nth-child(1) { width: 6.5% !important; }',
            '.weekly-report-table th:nth-child(2), .weekly-report-table td:nth-child(2) { width: 5.5% !important; text-align: right; }',
            '.weekly-report-table th:nth-child(3), .weekly-report-table td:nth-child(3) { width: 5.5% !important; text-align: right; }',
            '.weekly-report-table th:nth-child(4), .weekly-report-table td:nth-child(4) { width: 5.5% !important; text-align: right; }',
            '.weekly-report-table th:nth-child(5), .weekly-report-table td:nth-child(5) { width: 5.5% !important; text-align: right; }',
            '.weekly-report-table th:nth-child(6), .weekly-report-table td:nth-child(6) { width: 13% !important; }',
            '.weekly-report-table th:nth-child(7), .weekly-report-table td:nth-child(7) { width: 29% !important; }',
            '.weekly-report-table th:nth-child(8), .weekly-report-table td:nth-child(8) { width: 29.5% !important; }',
            '.chart-guide, .timeline-legend { font-size: 8.5px !important; gap: 0.5rem !important; margin-top: 4px !important; }',
            '.weekly-timeline-sample { page-break-before: always; }',
            '@media print { body { padding: 0; } .weekly-timeline-sample { break-inside: avoid; page-break-inside: avoid; } }'
        ].join('\n');
    }

    function waitForPrintAssets(printDocument) {
        const assets = Array.from(printDocument.querySelectorAll('link[rel="stylesheet"], img'));
        if (assets.length === 0) {
            return Promise.resolve();
        }

        return Promise.all(assets.map(function (asset) {
            return new Promise(function (resolve) {
                if (asset.tagName === 'IMG' && asset.complete) {
                    resolve();
                    return;
                }

                asset.addEventListener('load', resolve, { once: true });
                asset.addEventListener('error', resolve, { once: true });
                setTimeout(resolve, 1200);
            });
        }));
    }

    function exportSamplesToPdf() {
        const sections = [
            document.querySelector('.weekly-report-sample'),
            document.querySelector('.weekly-timeline-sample')
        ].filter(Boolean);

        if (sections.length === 0) {
            alert('出力対象のセクションが見つかりません。');
            return;
        }

        const printWindow = window.open('', '_blank');
        if (!printWindow) {
            alert('ポップアップがブロックされました。ブラウザ設定で許可してください。');
            return;
        }

        const now = new Date();
        const dateText = now.getFullYear() + '-'
            + String(now.getMonth() + 1).padStart(2, '0') + '-'
            + String(now.getDate()).padStart(2, '0');
        const printDocument = printWindow.document;

        printDocument.open();
        printDocument.write('<!DOCTYPE html><html lang="ja"><head><meta charset="utf-8"><title></title></head><body></body></html>');
        printDocument.close();
        printDocument.title = 'Phycock 週次レポート ' + dateText;

        document.querySelectorAll('link[rel="stylesheet"]').forEach(function (sourceLink) {
            const link = printDocument.createElement('link');
            link.rel = 'stylesheet';
            link.href = sourceLink.href;
            printDocument.head.appendChild(link);
        });

        const style = printDocument.createElement('style');
        style.textContent = buildPrintCss();
        printDocument.head.appendChild(style);

        const title = printDocument.createElement('h1');
        title.className = 'print-title';
        title.textContent = printDocument.title;
        printDocument.body.appendChild(title);

        const meta = printDocument.createElement('p');
        meta.className = 'print-meta';
        meta.textContent = '出力日時: ' + now.toLocaleString('ja-JP') + ' / これはサンプルデータです。';
        printDocument.body.appendChild(meta);

        sections.forEach(function (section) {
            printDocument.body.appendChild(printDocument.importNode(cloneSectionForPrint(section), true));
        });

        waitForPrintAssets(printDocument).then(function () {
            printWindow.focus();
            printWindow.print();
        });
    }

    const exportButton = document.getElementById('exportSamplePdf');
    if (exportButton) {
        exportButton.addEventListener('click', exportSamplesToPdf);
    }
}());
