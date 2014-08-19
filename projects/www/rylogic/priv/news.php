<div id="news">
	<div class="title">News</div>
	<div class="list">
		<?php
		$link = new mysqli($host,$db_user,$db_pass,$db_name);
		if ($link->connect_error) die('connect error ('.$link->connect_errno.') '.$link->connect_error);
		$msgs = $link->query("select * from news order by Date desc limit 0, 10") or die("error".mysqli_error($link));
		while ($row = mysqli_fetch_array($msgs))
		{
			?>
			<div class="news_item">
				<div class="news_item_date"><?=$row["Date"];?></div>
				<div class="news_item_msg"><?=$row["Message"];?></div>
			</div>
			<?php
		}
		$link->close();
		?>
	</div>
</div>