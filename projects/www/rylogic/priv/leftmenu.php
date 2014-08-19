<?php
$items = array(
	"home"=>"index.php",
	"download"=>"index.php",
	"features"=>"index.php",
	"manual"=>"index.php",
	"contact"=>"contact.php");
?>
<div id='leftmenu'>
	<div class='list'>
		<?php foreach ($items as $name=>$link): ?>
			<div class='list_item'>
				<a class='menu_link' href='<?=$link?>'><?=$name?></a>
			</div>
		<?php endforeach;?>
	</div>
</div>
