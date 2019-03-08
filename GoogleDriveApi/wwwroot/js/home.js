$(document).ready(() => {
    $(".download").click(e => {
        if (!confirm("Are you sure to delete this file?")) {
            e.preventDefault();
        }
    });

    $("#file").change(event => {
        let fileName = event.target.files[0].name;
        $("#file-name").html(fileName);
    });
});