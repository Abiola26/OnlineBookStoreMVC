var dataTable;

$(document).ready(function () {
    $('#tblData').DataTable({
        paging: true,
        searching: true,
        ordering: true,
        info: true
    });
});